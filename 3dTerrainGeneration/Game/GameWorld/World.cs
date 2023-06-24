using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.Options;
using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Engine.World;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static OpenTK.Mathematics.MathHelper;

namespace _3dTerrainGeneration.Game.GameWorld
{
    internal class World : IWorld
    {
        private static readonly Vector3 v0 = new Vector3(Chunk.CHUNK_SIZE, 0, 0),
                                v1 = new Vector3(Chunk.CHUNK_SIZE, 0, Chunk.CHUNK_SIZE),
                                v2 = new Vector3(0, 0, Chunk.CHUNK_SIZE),
                                v3 = new Vector3(0, Chunk.CHUNK_SIZE, 0),
                                v4 = new Vector3(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, 0),
                                v5 = new Vector3(Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE),
                                v6 = new Vector3(0, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE);

        private static readonly Vector3I[] ChunkIterationOrder;
        private static readonly float SunPitch = DegreesToRadians(25);

        private static readonly int viewDistanceChunks = 0;
        private static readonly int totalChunkCount = 0;

        private Dictionary<Vector3I, Chunk> chunks;

        private TerrainGenerator terraingGenerator;

        public double Time = 1000 * 420;
        public Vector3 SunPosition { get; set; }

        private Queue<Vector3I> chunkQueue;
        private Queue<Chunk> chunkMeshQueue;

        static World()
        {
            OptionManager.Instance.RegisterOption("World", "View Distance", new DoubleOption(128, 2048, 512));

            viewDistanceChunks = (int)OptionManager.Instance["World", "View Distance"] / Chunk.CHUNK_SIZE * 2;
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

            ChunkIterationOrder = order.ToArray();
        }

        public World()
        {
            chunks = new Dictionary<Vector3I, Chunk>();
            chunkQueue = new Queue<Vector3I>();
            chunkMeshQueue = new Queue<Chunk>();
            terraingGenerator = new TerrainGenerator();

            ChunkGenerationWorker();
            MeshGeneratorWorker();
        }

        private void ChunkGenerationWorker()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);

                    Vector3I chunkPosition;
                    lock (chunkQueue)
                    {
                        if (chunkQueue.Count == 0) continue;

                        chunkPosition = chunkQueue.Peek();
                    }

                    Chunk chunk;
                    if (chunks.ContainsKey(chunkPosition))
                    {
                        chunk = chunks[chunkPosition];
                    }
                    else
                    {
                        chunk = new Chunk(this, chunkPosition.X, chunkPosition.Y, chunkPosition.Z);
                    }

                    if (!chunk.HasTerrain)
                    {
                        terraingGenerator.GenerateTerrain(chunk);
                    }

                    terraingGenerator.Populate(chunk, chunks);
                    chunk.IsRemeshingNeeded = true;

                    lock (chunkQueue)
                    {
                        if (!chunks.ContainsKey(chunkPosition))
                        {
                            chunks[chunkPosition] = chunk;
                        }
                        chunkQueue.Dequeue();
                    }
                }
            });
        }

        public void MeshGeneratorWorker()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(1);

                    Chunk chunk;
                    lock (chunkMeshQueue)
                    {
                        if (chunkMeshQueue.Count == 0) continue;

                        chunk = chunkMeshQueue.Peek();
                    }

                    chunk.Mesh();

                    lock (chunkMeshQueue)
                    {
                        chunkMeshQueue.Dequeue();
                    }
                }
            });
        }

        public void Tick(Vector3 origin)
        {
            lock (chunkQueue)
            {
                for (int i = 0; i < totalChunkCount; i++)
                {
                    Vector3I indexPosition = ChunkIterationOrder[i];
                    Vector3I originChunkCoord = new Vector3I((int)origin.X, (int)origin.Y, (int)origin.Z) / Chunk.CHUNK_SIZE;
                    Vector3I chunkPosition = indexPosition + originChunkCoord;

                    if (chunkMeshQueue.Count < 5 && chunks.ContainsKey(chunkPosition) && chunks[chunkPosition].IsRemeshingNeeded && !chunkMeshQueue.Contains(chunks[chunkPosition]))
                    {
                        chunkMeshQueue.Enqueue(chunks[chunkPosition]);
                    }

                    if (chunkQueue.Count < 5 && (!chunks.ContainsKey(chunkPosition) || !chunks[chunkPosition].IsPopulated) && !chunkQueue.Contains(chunkPosition))
                    {
                        chunkQueue.Enqueue(chunkPosition);
                    }
                }
            }
        }

        public uint GetBlockAt(double x, double y, double z)
        {
            double chunkX = x / Chunk.CHUNK_SIZE;
            double chunkY = y / Chunk.CHUNK_SIZE;
            double chunkZ = z / Chunk.CHUNK_SIZE;

            Vector3I chunkCoord = new Vector3I((int)Math.Floor(chunkX), (int)Math.Floor(chunkY), (int)Math.Floor(chunkZ));
            if (!chunks.ContainsKey(chunkCoord)) return (uint)(y < 0 ? 1 : 0);

            Chunk chunk = chunks[chunkCoord];
            if (chunk == null) return (uint)(y < 0 ? 1 : 0);

            x %= Chunk.CHUNK_SIZE;
            y %= Chunk.CHUNK_SIZE;
            z %= Chunk.CHUNK_SIZE;

            if (x < 0)
            {
                x += Chunk.CHUNK_SIZE;
            }

            if (y < 0)
            {
                y += Chunk.CHUNK_SIZE;
            }

            if (z < 0)
            {
                z += Chunk.CHUNK_SIZE;
            }

            return chunk.GetBlockAt((int)Math.Floor(x), (int)Math.Floor(y), (int)Math.Floor(z));
        }

        public int Render(Camera camera)
        {
            //Time += fT * 5000;
            //Time += fT * 20000;
            Time = 800000;
            float t = (float)(Time / 1000 / 1440 % 1);

            double X = MathF.Cos(t * 2 * MathF.PI - MathF.PI * .5f) * MathF.Cos(SunPitch);
            double Y = MathF.Sin(t * 2 * MathF.PI - MathF.PI * .5f) * MathF.Cos(SunPitch);
            double Z = MathF.Sin(SunPitch);

            SunPosition = new Vector3((float)X, (float)Y, (float)Z);

            return RenderWorld(camera.Position, camera.GetViewMatrix() * camera.GetProjectionMatrix(), false);
        }

        public int RenderWorld(Vector3 origin, Matrix4x4 mat, bool ortho, Vector3 viewDirection = default)
        {
            Vector3I originChunkCoord = new Vector3I((int)origin.X, (int)origin.Y, (int)origin.Z) / Chunk.CHUNK_SIZE;
            
            for (int i = 0; i < totalChunkCount; i++)
            {
                Vector3I indexPosition = ChunkIterationOrder[ortho ? totalChunkCount - i - 1 : i];
                Vector3I chunkPosition = indexPosition + originChunkCoord;

                if (ShouldRender(new Vector3(chunkPosition.X, chunkPosition.Y, chunkPosition.Z) * Chunk.CHUNK_SIZE, mat))
                {
                    if (chunks.ContainsKey(chunkPosition))
                    {
                        Chunk chunk = chunks[chunkPosition];

                        float distance = indexPosition.Length();

                        int lod = (int)Math.Clamp(distance / viewDistanceChunks * Chunk.LOD_COUNT, 0, Chunk.LOD_COUNT);
                        chunk.Render(lod, ortho, viewDirection);
                    }
                }
            }

            return 0;
        }

        private bool ShouldRender(Vector3 pos, Matrix4x4 mat)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

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
