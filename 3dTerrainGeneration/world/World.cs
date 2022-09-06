using _3dTerrainGeneration.entity;
using _3dTerrainGeneration.network;
using _3dTerrainGeneration.rendering;
using System.Numerics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrainServer.network;
using System.Threading;
using System.Runtime.Intrinsics.X86;

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
        private Vector3[] iterationOrder;
        private double SunPitch = ToRadians(25);
        private object delayLock = new object();

        public static float renderDist = GameSettings.VIEW_DISTANCE;
        public static int size = (int)(renderDist / Chunk.Size * 2);
        
        public List<Structure> structures = new List<Structure>();
        public Dictionary<EntityType, Dictionary<int, DrawableEntity>> entities = new Dictionary<EntityType, Dictionary<int, DrawableEntity>>();
        public ConcurrentDictionary<Vector3, Chunk> chunks = new ConcurrentDictionary<Vector3, Chunk>();
        public Player player;
        public double Time = 1000 * 420;
        public object structureLock = new object();
        public static Vector3 sunPos = new Vector3(0, 1, 0);
        public Network network;
        public static GameRenderer gameRenderer;
        public Farlands Farlands;
        public static int genDelay = 0;

        int chunksLen = 0;
        public World()
        {
            gameRenderer = new GameRenderer();
            Farlands = new Farlands();
            player = new Player(this);
            player.IsResponsible = true;

            foreach (EntityType entityType in Enum.GetValues<EntityType>())
            {
                entities[entityType] = new Dictionary<int, DrawableEntity>();
            }

            chunksLen = size * size * size;
            iterationOrder = new Vector3[size * size * size];

            List<Vector3> order = new List<Vector3>();
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        order.Add(new Vector3(x - size / 2, y - size / 2, z - size / 2));
                    }
                }
            }

            order.Sort((a, b)=> { return (int)((a.Length() - b.Length()) * 1000); });

            //order.Reverse();
            iterationOrder = order.ToArray();
        }

        public void SpawnEntity(int entityId, EntityType entityType, float x, float y, float z, float mx, float my, float mz)
        {
            entities[entityType][entityId] = entityType.GetEntity(this, new Vector3(x, y, z), new Vector3(mx, my, mz), entityId);
        }

        public void DespawnEntity(int entityId)
        {
            foreach (Dictionary<int, DrawableEntity> item in entities.Values)
            {
                if(item.ContainsKey(entityId))
                {
                    item[entityId].Despawn();

                    if (item.Remove(entityId))
                    {
                        break;
                    }
                }
            }
        }

        public void SetResponsible(int entityId)
        {
            foreach (Dictionary<int, DrawableEntity> item in entities.Values)
            {
                if (item.ContainsKey(entityId))
                {
                    item[entityId].IsResponsible = true;
                    break;
                }
            }
        }

        public int GetEntityCount()
        {
            int count = 0;
            foreach (var e in entities)
            {
                count += e.Value.Count;
            }

            return count;
        }

        public int GetEntityId()
        {
            int c = GetEntityCount() + 1;
            for (int i = 0; i < c; i++)
            {
                foreach (Dictionary<int, DrawableEntity> item in entities.Values)
                {
                    if (item.ContainsKey(i))
                    {
                        goto o;
                    }
                }

                return i;

                o: {

                }
            }

            throw new Exception("Cannot generate an entity id!");
        }

        private static double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public List<DrawableEntity> GetEntities()
        {
            List<DrawableEntity> _entities = new List<DrawableEntity>();
            foreach (var item in entities)
            {
                _entities.AddRange(item.Value.Values);
            }

            return _entities;
        }

        public List<DrawableEntity> GetEntities(EntityType type)
        {
            List<DrawableEntity> l = entities[type].Values.ToList();

            if (type == EntityType.Player)
            {
                l.Add(player);
            }

            return l;
        }

        public void Tick(double fT)
        {
            player.PhisycsUpdate(fT);
            foreach (Dictionary<int, DrawableEntity> item in entities.Values)
            {
                foreach (DrawableEntity entity in item.Values)
                {
                    entity.PhisycsUpdate(fT);
                }
            }
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

            Vector3 chunkCoord = new Vector3((int)Math.Floor(chunkX), (int)Math.Floor(chunkY), (int)Math.Floor(chunkZ));
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

        public int Render(FragmentShader shader, FragmentShader post, Camera camera, double fT, double frameDelta)
        {
            //Time += fT * 1000;
            //Time += fT * 10000;
            Time = 520000;
            double t = Time / 1000 / 1440 % 1;

            double X = Math.Cos(t * 2 * Math.PI - Math.PI * .5) * Math.Cos(SunPitch);
            double Y = Math.Sin(t * 2 * Math.PI - Math.PI * .5) * Math.Cos(SunPitch);
            double Z = Math.Sin(SunPitch);

            sunPos = new Vector3((float)X, (float)Y, (float)Z);

            List<Vector3> removeChunks = new List<Vector3>();
            foreach (var chunk in chunks)
            {
                if (Math.Abs(chunk.Key.X - (int)(camera.Position.X / Chunk.Size)) > size / 2 + 1 || 
                    Math.Abs(chunk.Key.Y - (int)(camera.Position.Y / Chunk.Size)) > size / 2 + 1 ||
                    Math.Abs(chunk.Key.Z - (int)(camera.Position.Z / Chunk.Size)) > size / 2 + 1)
                {
                    removeChunks.Add(chunk.Key);
                    //Console.WriteLine("removing chunk {0} {1} {2} from memory", chunk.Key.X, chunk.Key.Y, chunk.Key.Z);
                }
            }

            for (int i = 0; i < removeChunks.Count; i++)
            {
                Chunk ch;
                chunks.TryRemove(removeChunks[i], out ch);
                if(ch != null)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        gameRenderer.FreeMemory(ch.drawCall[j]);
                    }
                }
            }
            lock (structureLock)
            {
                structures.RemoveAll((structure) =>
                {
                    return structure.blocks == 0 || Math.Abs(structure.xPos - (int)camera.Position.X) > renderDist + Chunk.Size * 2 ||
                        Math.Abs(structure.yPos - (int)camera.Position.Y) > renderDist + Chunk.Size * 2 ||
                        Math.Abs(structure.zPos - (int)camera.Position.Z) > renderDist + Chunk.Size * 2;
                });
            }

            return RenderWorld(camera.Position, camera.GetViewMatrix() * camera.GetProjectionMatrix(), shader, true, false, frameDelta);

            //System.Diagnostics.Debug.WriteLine("Verticles renderd: " + verticlesRendered);
        }

        public int RenderWorld(Vector3 Position, Matrix4x4 mat, FragmentShader shader, bool gen, bool ortho, double frameDelta)
        {
            for (int i = 0; i < chunksLen; i++)
            {
                Vector3 iPos = iterationOrder[i];
                int x = (int)(iPos.X + (int)Position.X / Chunk.Size);
                int y = (int)(iPos.Y + (int)Position.Y / Chunk.Size);
                int z = (int)(iPos.Z + (int)Position.Z / Chunk.Size);

                Vector3 v = new Vector3(x, y, z);
                if(ShouldRender(v * Chunk.Size, mat))
                {
                    if (chunks.ContainsKey(v))
                    {
                        if (chunks[v] != null)
                        {
                            Chunk chunk = chunks[v];

                            Vector3 chunkDelta = v - (new Vector3(Position.X, Position.Y, Position.Z) / Chunk.Size);
                            float distance = chunkDelta.Length();

                            int lod = (int)Math.Clamp(distance / size * Chunk.lodCount, 0, Chunk.lodCount);
                            chunk.Render(lod, ortho);
                        }
                    }
                    else if (gen && !TryGenChunk(x, y, z))
                    {
                        gen = false;
                    }
                }
            }

            RenderEntities(frameDelta);
            Farlands.Render();

            gameRenderer.Draw(shader);

            return 0;
        }

        private static readonly Vector3 v0 = new Vector3(Chunk.Size, 0, 0),
                                        v1 = new Vector3(Chunk.Size, 0, Chunk.Size),
                                        v2 = new Vector3(0, 0, Chunk.Size),
                                        v3 = new Vector3(0, Chunk.Size, 0),
                                        v4 = new Vector3(Chunk.Size, Chunk.Size, 0),
                                        v5 = new Vector3(Chunk.Size, Chunk.Size, Chunk.Size),
                                        v6 = new Vector3(0, Chunk.Size, Chunk.Size);

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

            if(minY > 1 || minX > 1 || maxX < -1 || maxY < -1)
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

        private bool TryGenChunk(float x, float y, float z)
        {
            if (genDelay < GameSettings.MAX_CORES)
            {
                int xC = (int)x;
                int yC = (int)y;
                int zC = (int)z;
                chunks[new Vector3(xC, yC, zC)] = null;

                genDelay++;
                Task.Run(() =>
                {
                    generate(xC, yC, zC);
                }).ContinueWith((task) =>
                {
                    if (task.IsFaulted)
                    {
                        Console.WriteLine(task.Exception);
                        Window.message = "Error generating chunk!";
                    }
                });

                return true;
            }
            return false;
        }

        private void RenderEntities(double frameDelta)
        {
            player.Render(frameDelta);

            foreach (Dictionary<int, DrawableEntity> item in entities.Values)
            {
                foreach (DrawableEntity entity in item.Values)
                {
                    entity.Render(frameDelta);
                }
            }
        }

        private void generate(int x, int y, int z)
        {
            Chunk ch = new Chunk(x, y, z);

            chunks[new Vector3(x, y, z)] = ch;
            lock (delayLock)
            {
                genDelay--;
            }
        }
    }
}
