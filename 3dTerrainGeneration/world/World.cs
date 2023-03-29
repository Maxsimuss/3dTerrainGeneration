using _3dTerrainGeneration.entity;
using _3dTerrainGeneration.network;
using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.util;
using CSCore.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TerrainServer.network;
using static OpenTK.Mathematics.MathHelper;

namespace _3dTerrainGeneration.world
{
    public struct EntitySpawn
    {
        public EntitySpawn(EntityType type, float x, float y, float z)
        {
            this.type = type;
            this.x = x; this.y = y; this.z = z;
        }

        public float x, y, z;
        public EntityType type;
    }

    public class World
    {
        private static readonly Vector3 v0 = new Vector3(Chunk.Size, 0, 0),
                                v1 = new Vector3(Chunk.Size, 0, Chunk.Size),
                                v2 = new Vector3(0, 0, Chunk.Size),
                                v3 = new Vector3(0, Chunk.Size, 0),
                                v4 = new Vector3(Chunk.Size, Chunk.Size, 0),
                                v5 = new Vector3(Chunk.Size, Chunk.Size, Chunk.Size),
                                v6 = new Vector3(0, Chunk.Size, Chunk.Size);

        public Network network;

        private static readonly Vector3I[] ChunkIterationOrder;
        private static readonly float SunPitch = DegreesToRadians(25);

        private static readonly int viewDistanceBlocks = 0;
        private static readonly int viewDistanceChunks = 0;
        private static readonly int totalChunkCount = 0;

        private ConcurrentDictionary<Vector3I, Chunk> chunks;
        private TerrainGenerator terraingGenerator;

        public Player player;

        public double Time = 1000 * 420;
        public Vector3 SunPosition = new Vector3(0, 1, 0);

        private Queue<Vector3I> chunkQueue;

        static World()
        {
            viewDistanceBlocks = GameSettings.Instance.View_Distance;
            viewDistanceChunks = viewDistanceBlocks / GameSettings.CHUNK_SIZE * 2;
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
            player = new Player(this);
            player.IsResponsible = true;

            chunks = new ConcurrentDictionary<Vector3I, Chunk>();
            chunkQueue = new Queue<Vector3I>();
            terraingGenerator = new TerrainGenerator();

            ChunkGenerationWorker();
            //foreach (EntityType entityType in Enum.GetValues<EntityType>())
            //{
            //    entities[entityType] = new Dictionary<int, DrawableEntity>();
            //}
        }

        public void SpawnEntity(int entityId, EntityType entityType, float x, float y, float z, float mx, float my, float mz)
        {
            //entities[entityType][entityId] = entityType.GetEntity(this, new Vector3(x, y, z), new Vector3(mx, my, mz), entityId);
        }

        public void DespawnEntity(int entityId)
        {
            //foreach (Dictionary<int, DrawableEntity> item in entities.Values)
            //{
            //    if (item.ContainsKey(entityId))
            //    {
            //        item[entityId].Despawn();

            //        if (item.Remove(entityId))
            //        {
            //            break;
            //        }
            //    }
            //}
        }

        public void SetResponsible(int entityId)
        {
            //foreach (Dictionary<int, DrawableEntity> item in entities.Values)
            //{
            //    if (item.ContainsKey(entityId))
            //    {
            //        item[entityId].IsResponsible = true;
            //        break;
            //    }
            //}
        }

        public int GetEntityCount()
        {
            //int count = 0;
            //foreach (var e in entities)
            //{
            //    count += e.Value.Count;
            //}

            //return count;

            return 0;
        }

        public int GetEntityId()
        {
            //int c = GetEntityCount() + 1;
            //for (int i = 0; i < c; i++)
            //{
            //    foreach (Dictionary<int, DrawableEntity> item in entities.Values)
            //    {
            //        if (item.ContainsKey(i))
            //        {
            //            goto o;
            //        }
            //    }

            //    return i;

            //o:
            //    {

            //    }
            //}

            //throw new Exception("Cannot generate an entity id!");

            return 0;
        }

        public List<EntityBase> GetEntities()
        {
            //List<DrawableEntity> _entities = new List<DrawableEntity>();
            //foreach (var item in entities)
            //{
            //    _entities.AddRange(item.Value.Values);
            //}

            //return _entities;

            return null;
        }

        public List<EntityBase> GetEntities(EntityType type)
        {
            //List<DrawableEntity> l = entities[type].Values.ToList();

            //if (type == EntityType.Player)
            //{
            //    l.Add(player);
            //}

            //return l;

            return null;
        }

        private void ChunkGenerationWorker()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);

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

        public void Tick(double fT)
        {
            Vector3 origin = player.GetEyePosition(0);

            lock (chunkQueue)
            {
                if (chunkQueue.Count < 5)
                    for (int i = 0; i < totalChunkCount; i++)
                    {
                        Vector3I indexPosition = ChunkIterationOrder[i];
                        Vector3I originChunkCoord = new Vector3I((int)origin.X, (int)origin.Y, (int)origin.Z) / GameSettings.CHUNK_SIZE;
                        Vector3I chunkPosition = indexPosition + originChunkCoord;

                        if ((!chunks.ContainsKey(chunkPosition) || !chunks[chunkPosition].IsPopulated) && !chunkQueue.Contains(chunkPosition))
                        {
                            chunkQueue.Enqueue(chunkPosition);
                            break;
                        }
                    }
            }

            player.PhisycsUpdate(fT);

            //foreach (Dictionary<int, DrawableEntity> item in entities.Values)
            //{
            //    foreach (DrawableEntity entity in item.Values)
            //    {
            //        entity.PhisycsUpdate(fT);
            //    }
            //}
        }

        public bool GetBlockAt(Vector3 pos)
        {
            return GetBlockAt(pos.X, pos.Y, pos.Z);
        }

        public bool GetBlockAt(double x, double y, double z)
        {
            double chunkX = x / Chunk.Size;
            double chunkY = y / Chunk.Size;
            double chunkZ = z / Chunk.Size;

            Vector3I chunkCoord = new Vector3I((int)Math.Floor(chunkX), (int)Math.Floor(chunkY), (int)Math.Floor(chunkZ));
            if (!chunks.ContainsKey(chunkCoord)) return y < 0;

            Chunk chunk = chunks[chunkCoord];
            if (chunk == null) return y < 0;

            x %= Chunk.Size;
            y %= Chunk.Size;
            z %= Chunk.Size;

            if (x < 0)
            {
                x += Chunk.Size;
            }

            if (y < 0)
            {
                y += Chunk.Size;
            }

            if (z < 0)
            {
                z += Chunk.Size;
            }

            return chunk.GetBlockAt((int)Math.Floor(x), (int)Math.Floor(y), (int)Math.Floor(z));
        }

        public int Render(FragmentShader shader, Camera camera, double fT, double frameDelta)
        {
            //int id = GetEntityId();
            //SpawnEntity(id, EntityType.Frog, (float)player.x, (float)player.y, (float)player.z, 0, 0, 0);
            //SetResponsible(id);

            //Time += fT * 5000;
            //Time += fT * 20000;
            Time = 800000;
            float t = (float)(Time / 1000 / 1440 % 1);

            double X = MathF.Cos(t * 2 * MathF.PI - MathF.PI * .5f) * MathF.Cos(SunPitch);
            double Y = MathF.Sin(t * 2 * MathF.PI - MathF.PI * .5f) * MathF.Cos(SunPitch);
            double Z = MathF.Sin(SunPitch);

            SunPosition = new Vector3((float)X, (float)Y, (float)Z);

            return RenderWorld(camera.Position, camera.GetViewMatrix() * camera.GetProjectionMatrix(), shader, false, frameDelta);
        }

        public int RenderWorld(Vector3 origin, Matrix4x4 mat, FragmentShader shader, bool ortho, double frameDelta)
        {
            for (int i = 0; i < totalChunkCount; i++)
            {
                Vector3I indexPosition = ChunkIterationOrder[ortho ? totalChunkCount - i - 1 : i];
                Vector3I originChunkCoord = new Vector3I((int)origin.X, (int)origin.Y, (int)origin.Z) / GameSettings.CHUNK_SIZE;
                Vector3I chunkPosition = indexPosition + originChunkCoord;

                if (ShouldRender(new Vector3(chunkPosition.X, chunkPosition.Y, chunkPosition.Z) * GameSettings.CHUNK_SIZE, mat))
                {
                    if (chunks.ContainsKey(chunkPosition))
                    {
                        Chunk chunk = chunks[chunkPosition];

                        float distance = indexPosition.Length();

                        int lod = (int)Math.Clamp(distance / viewDistanceChunks * Chunk.LodCount, 0, Chunk.LodCount);
                        chunk.Render(lod, ortho);
                    }
                }
            }

            RenderEntities(frameDelta);

            GameRenderer.Instance.Draw(shader);

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

        private void RenderEntities(double frameDelta)
        {
            player.Render(frameDelta);

            //foreach (Dictionary<int, DrawableEntity> item in entities.Values)
            //{
            //    foreach (DrawableEntity entity in item.Values)
            //    {
            //        entity.Render(frameDelta);
            //    }
            //}
        }
    }
}
