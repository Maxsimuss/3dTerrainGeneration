using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.Graphics.UI.Screens;
using _3dTerrainGeneration.Engine.Options;
using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using _3dTerrainGeneration.Game.GameWorld.Workers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Game.GameWorld
{
    internal class ChunkManager
    {
        private static readonly Vector3 v0 = new Vector3(Chunk.CHUNK_SIZE, 0, 0),
                        v1 = new Vector3(Chunk.CHUNK_SIZE, 0, Chunk.CHUNK_SIZE),
                        v2 = new Vector3(0, 0, Chunk.CHUNK_SIZE),
                        v3 = new Vector3(0, Chunk.CHUNK_SIZE, 0),
                        v4 = new Vector3(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, 0),
                        v5 = new Vector3(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE),
                        v6 = new Vector3(0, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE);

        private ConcurrentDictionary<Vector3I, Chunk> chunks;
        private PackedArray<Chunk> chunkArray;
        private TerrainGenerator terrainGenerator;
        private TerrainPopulationWorker populationWorker;
        private TerrainGenerationWorker[] generationWorkers = new TerrainGenerationWorker[3];
        private ChunkMeshWorker[] meshWorkers = new ChunkMeshWorker[4];
        private World world;
        private Vector3I originChunkCoord;
        private int totalChunkCount;
        private readonly Vector3I[] chunkIterationOrder;
        private ConcurrentQueue<Chunk> needPopulation = new ConcurrentQueue<Chunk>();
        private ConcurrentQueue<Chunk> unloadedChunks = new ConcurrentQueue<Chunk>();
        private ConcurrentQueue<Chunk> unloadQueue = new ConcurrentQueue<Chunk>();

        public ChunkManager(World world)
        {
            this.world = world;
            OptionManager.Instance.RegisterOption("World", "View Distance", new DoubleOption(128, 2048, 1024));
            OptionManager.Instance.RegisterOption("World", "LOD Bias", new DoubleOption(100, 2000, 500));

            int viewDistanceChunks = (int)OptionManager.Instance["World", "View Distance"] / Chunk.CHUNK_SIZE * 2;
            totalChunkCount = viewDistanceChunks * viewDistanceChunks * viewDistanceChunks;

            List<Vector3I> order = new List<Vector3I>();
            for (int x = 0; x < viewDistanceChunks; x++)
            {
                for (int y = 0; y < viewDistanceChunks; y++)
                {
                    for (int z = 0; z < viewDistanceChunks; z++)
                    {
                        order.Add(new Vector3I(x - viewDistanceChunks / 2, y - viewDistanceChunks / 2, z - viewDistanceChunks / 2));
                    }
                }
            }
            order.Sort((a, b) => a.LengthSq() - b.LengthSq());

            chunkIterationOrder = order.ToArray();

            chunks = new ConcurrentDictionary<Vector3I, Chunk>();
            chunkArray = new PackedArray<Chunk>(totalChunkCount * 2);

            terrainGenerator = new TerrainGenerator();
            populationWorker = new TerrainPopulationWorker(this, terrainGenerator, needPopulation.Enqueue);

            for (int i = 0; i < meshWorkers.Length; i++)
            {
                meshWorkers[i] = new ChunkMeshWorker();
            }

            for (int i = 0; i < generationWorkers.Length; i++)
            {
                generationWorkers[i] = new TerrainGenerationWorker(terrainGenerator, needPopulation.Enqueue);
            }

            SceneRenderer.Instance.EnsureNotNull();

            RunGeneration();
        }

        private void AddChunk(Chunk chunk)
        {
            // stall idc
            while (!chunks.TryAdd(chunk.Position, chunk)) { }

            lock (chunkArray)
            {
                chunkArray.Insert(chunk);
            }
        }

        public bool IsChunkLoadedAt(Vector3I chunkPosition)
        {
            return chunks.ContainsKey(chunkPosition) && (chunks[chunkPosition].State & ChunkState.IsPopulated) != 0;
        }

        public Chunk GetChunkAt(Vector3I pos)
        {
            return chunks.ContainsKey(pos) ? chunks[pos] : null;
        }

        private bool WorkersBusy(IWorker<Chunk>[] workers)
        {
            foreach (IWorker<Chunk> worker in workers)
            {
                if (!worker.IsBusy())
                {
                    return false;
                }
            }

            return true;
        }

        private void SubmitWork(IWorker<Chunk>[] workers, Chunk chunk)
        {
            foreach (IWorker<Chunk> worker in workers)
            {
                if (!worker.IsBusy())
                {
                    worker.SubmitWork(chunk);
                    return;
                }
            }

            workers[0].SubmitWork(chunk);
        }

        public void RunGeneration()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    DebugHud.DebugInfo["Gen"] = WorkersBusy(generationWorkers).ToString();
                    for (int i = 0; i < totalChunkCount && !WorkersBusy(generationWorkers); i++)
                    {
                        Vector3I indexPosition = chunkIterationOrder[i];
                        Vector3I chunkPosition = indexPosition + originChunkCoord;

                        Chunk chunk = null;
                        if (!chunks.ContainsKey(chunkPosition))
                        {
                            chunk = new Chunk(world, chunkPosition.X, chunkPosition.Y, chunkPosition.Z);
                            AddChunk(chunk);
                            SubmitWork(generationWorkers, chunk);
                        }
                    }

                    DebugHud.DebugInfo["Pop"] = populationWorker.IsBusy().ToString();
                    while (!populationWorker.IsBusy() && needPopulation.Count > 0 && needPopulation.TryDequeue(out Chunk chunk))
                    {
                        if ((chunk.State & (ChunkState.AwaitingTerrainPopulation | ChunkState.IsPopulated)) == 0 && (chunk.State & ChunkState.HasTerrain) != 0)
                        {
                            populationWorker.SubmitWork(chunk);
                        }
                    }

                    for (int i = 0; i < totalChunkCount && !populationWorker.IsBusy(); i++)
                    {
                        Vector3I indexPosition = chunkIterationOrder[i];
                        Vector3I chunkPosition = indexPosition + originChunkCoord;

                        if (chunks.ContainsKey(chunkPosition))
                        {
                            Chunk chunk = chunks[chunkPosition];
                            if ((chunk.State & (ChunkState.AwaitingTerrainPopulation | ChunkState.IsPopulated)) == 0 && (chunk.State & ChunkState.HasTerrain) != 0)
                            {
                                populationWorker.SubmitWork(chunk);
                            }
                        }
                    }

                    DebugHud.DebugInfo["Mesh"] = WorkersBusy(meshWorkers).ToString();
                    for (int i = 0; i < totalChunkCount && !WorkersBusy(meshWorkers); i++)
                    {
                        Vector3I indexPosition = chunkIterationOrder[i];
                        Vector3I chunkPosition = indexPosition + originChunkCoord;

                        if (chunks.ContainsKey(chunkPosition))
                        {
                            Chunk chunk = chunks[chunkPosition];

                            if ((chunk.State & ChunkState.AwaitingMeshGeneration) == 0 && (chunk.State & (ChunkState.NeedsRemeshing | ChunkState.HasTerrain | ChunkState.IsPopulated)) == (ChunkState.NeedsRemeshing | ChunkState.HasTerrain | ChunkState.IsPopulated))
                            {
                                SubmitWork(meshWorkers, chunk);
                            }
                        }
                    }

                    if (SceneRenderer.Instance.VramUsage >= SceneRenderer.Instance.VramAllocated * .9)
                    {
                        List<Chunk> orderedChunks = chunks.Values.ToList();
                        orderedChunks.Sort((c1, c2) => (c2 != null ? (originChunkCoord - c2.Position).LengthSq() : int.MaxValue) - (c1 != null ? (originChunkCoord - c1.Position).LengthSq() : int.MaxValue));

                        for (int i = 0; i < 1 && i < orderedChunks.Count; i++)
                        {
                            unloadQueue.Enqueue(orderedChunks[i]);
                        }
                    }

                    while (unloadedChunks.Count > 0 && unloadedChunks.TryDequeue(out Chunk chunk))
                    {
                        if (chunks.ContainsKey(chunk.Position))
                        {
                            while (!chunks.TryRemove(chunk.Position, out _)) { }
                        }
                    }

                    Thread.Sleep(1);
                }
            });
        }

        private void UnloadChunk(Chunk chunk)
        {
            chunk.Unload();
            unloadedChunks.Enqueue(chunk);
            lock (chunkArray)
            {
                chunkArray.Remove(chunk);
            }
        }

        public void RenderChunks(Vector3 origin, Matrix4x4 mat, bool ortho, Vector3 viewDirection = default)
        {
            double lodBias = (double)OptionManager.Instance["World", "LOD Bias"].Value;
            originChunkCoord = new Vector3I((int)origin.X, (int)origin.Y, (int)origin.Z) / Chunk.CHUNK_SIZE;

            while (unloadQueue.TryDequeue(out Chunk unloadChunk))
            {
                UnloadChunk(unloadChunk);
            }

            lock (chunkArray)
            {
                chunkArray.SortOneShot((c1, c2) => (c1 != null ? (originChunkCoord - c1.Position).LengthSq() : int.MaxValue) - (c2 != null ? (originChunkCoord - c2.Position).LengthSq() : int.MaxValue));

                for (int i = 0; i < chunkArray.Count; i++)
                {
                    Chunk chunk = chunkArray[i];

                    if (ShouldRender(new Vector3(chunk.X, chunk.Y, chunk.Z) * Chunk.CHUNK_SIZE, mat, out float screenPct))
                    {

                        if ((chunk.State & ChunkState.IsPopulated) != 0)
                        {
                            if (!ortho)
                            {
                                int lod = (int)Math.Ceiling(Math.Clamp(Math.Log(1f / lodBias / screenPct * Chunk.CHUNK_SIZE) / Math.Log(2), 0, Chunk.LOD_COUNT));
                                chunk.SetLOD(lod);
                            }

                            chunk.Render(ortho, viewDirection);
                        }
                    }
                }
            }
        }

        private bool ShouldRender(Vector3 pos, Matrix4x4 mat, out float sceernPct)
        {
            float minX = 2;
            float maxX = -2;
            float minY = 2;
            float maxY = -2;

            CheckPoint(pos);
            CheckPoint(pos + v0);
            CheckPoint(pos + v1);
            CheckPoint(pos + v2);
            CheckPoint(pos + v3);
            CheckPoint(pos + v4);
            CheckPoint(pos + v5);
            CheckPoint(pos + v6);

            if (minY > 1 || minX > 1 || maxX < -1 || maxY < -1)
            {
                sceernPct = 0;
                return false;
            }

            sceernPct = Math.Clamp(Math.Max(maxX - minX, maxY - minY) / 2, 0, 1);
            if (float.IsNaN(sceernPct))
            {
                return false;
            }
            return true;

            void CheckPoint(Vector3 p)
            {
                Vector4 ss = Vector4.Transform(new Vector4(p, 1), mat);
                ss /= ss.W;

                minX = Math.Min(ss.X, minX);
                maxX = Math.Max(ss.X, maxX);

                minY = Math.Min(ss.Y, minY);
                maxY = Math.Max(ss.Y, maxY);
            }
        }
    }
}
