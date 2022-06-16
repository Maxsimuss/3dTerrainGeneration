using _3dTerrainGeneration.audio;
using _3dTerrainGeneration.entity;
using _3dTerrainGeneration.network;
using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.util;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TerrainServer.network;

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


    public struct Buffer
    {
        public int VBO, VAO;

        public Buffer(int VBO, int VAO)
        {
            this.VBO = VBO;
            this.VAO = VAO;
        }
    }
    public class World
    {
        public static float renderDist = GameSettings.VIEW_DISTANCE;
        static int size = (int)(renderDist / Chunk.Size * 2);
        int[] VAOs = new int[size * size * size * 16];
        int[] VBOs = new int[size * size * size * 16];
        private List<Vector3> iterationOrder = new List<Vector3>();
        public List<Structure> structures = new List<Structure>();
        public Dictionary<EntityType, Dictionary<int, DrawableEntity>> entities = new Dictionary<EntityType, Dictionary<int, DrawableEntity>>();
        public Player player;

        public ConcurrentDictionary<Vector3, Chunk> chunks = new ConcurrentDictionary<Vector3, Chunk>();
        object queueLock = new object();
        object delayLock = new object();
        public object structureLock = new object();
        Queue<Buffer> buffers = new Queue<Buffer>();

        public Vector3 sunPos = new Vector3(0, 1, 0);

        private Shader gbuffer;
        public Network network;

        int chunksLen = 0;
        public World(Shader gbuffer, InstancedRenderer renderer)
        {
            this.gbuffer = gbuffer;
            player = new Player(this, GenerateVBO());
            player.IsResponsible = true;

            GL.GenBuffers(VBOs.Length, VBOs);
            GL.GenVertexArrays(VAOs.Length, VAOs);

            for (int i = 0; i < VBOs.Length; i++)
            {
                PrepareVBO(VBOs[i], VAOs[i]);

                buffers.Enqueue(new(VBOs[i], VAOs[i]));
            }

            foreach (EntityType entityType in Enum.GetValues<EntityType>())
            {
                entities[entityType] = new Dictionary<int, DrawableEntity>();
            }

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        iterationOrder.Add(new Vector3(x - size / 2, y - size / 2, z - size / 2));
                    }
                }
            }

            iterationOrder.Sort((a, b)=> { return (int)((a.Length  - b.Length) * 1000); });
            chunksLen = iterationOrder.Count;
            this.renderer = renderer;
        }

        private int GenerateVBO()
        {
            int VBO = GL.GenBuffer();
            int VAO = GL.GenVertexArray();

            PrepareVBO(VBO, VAO);

            return VBO;
        }

        private void PrepareVBO(int VBO, int VAO)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BindVertexArray(VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            var dataLocation = gbuffer.GetAttribLocation("aData");
            GL.EnableVertexAttribArray(dataLocation);
            GL.VertexAttribPointer(dataLocation, 4, VertexAttribPointerType.UnsignedShort, false, 4 * sizeof(ushort), 0);
            gbuffer.Use();
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

        public static void WriteBool(List<byte> data, bool val)
        {
            data.Add((byte)(val ? 1 : 0));
        }

        public static void WriteInt(List<byte> data, int val)
        {
            data.AddRange(BitConverter.GetBytes(val));
        }

        public static void WriteArray(List<byte> data, ushort[] val)
        {
            WriteInt(data, val.Length);

            for (int i = 0; i < val.Length; i++)
            {
                data.AddRange(BitConverter.GetBytes(val[i]));
            }
        }

        public static void WriteArray(List<byte> data, byte[] val)
        {
            WriteInt(data, val.Length);

            data.AddRange(val);
        }

        private static string GetChunkDir()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/3dterrain/maps/";
        }

        private static string GetChunkFile(int X, int Y, int Z)
        {
            return GetChunkDir() + X + "." + Y + "." + Z + ".ch";
        }

        private static int version = 1337;

        public static void Save(Chunk chunk)
        {
            Directory.CreateDirectory(GetChunkDir());
            string file = GetChunkFile(chunk.X, chunk.Y, chunk.Z);

            if (File.Exists(file))
            {
                byte[] read = File.ReadAllBytes(file);
                if (read.Length == 6)
                {
                    if(BitConverter.ToInt32(read) == version)
                        return;
                }
                else
                {
                    read = Decompress(read);
                    if (BitConverter.ToInt32(read) == version)
                        return;
                }
            }

            List<byte> data = new List<byte>();
            WriteInt(data, version);
            WriteBool(data, chunk.empty);
            WriteBool(data, chunk.full);

            if (!chunk.empty)
            {
                for (int i = 0; i < Chunk.lodCount; i++)
                {
                    WriteArray(data, chunk.mesh[i]);
                }
                WriteArray(data, chunk.blocks);
                WriteArray(data, chunk.sounds.ToArray());
                WriteArray(data, chunk.particles.ToArray());
                File.WriteAllBytes(file, Compress(data.ToArray()));
            }
            else
            {
                File.WriteAllBytes(file, data.ToArray());
            }
        }

        public static byte[] Compress(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }

        public static bool Load(Chunk chunk, World world)
        {
            string file = GetChunkFile(chunk.X, chunk.Y, chunk.Z);
            if (File.Exists(file))
            {
                byte[] data = File.ReadAllBytes(file);

                int offset = 0;

                if(data.Length == 6)
                {
                    if (BitConverter.ToInt32(data, offset) != version)
                    {
                        return false;
                    }
                    offset += 4;

                    chunk.empty = data[offset++] == 1;
                    chunk.full = data[offset++] == 1;

                    return true;
                }

                data = Decompress(data);

                if (BitConverter.ToInt32(data, offset) != version)
                {
                    return false;
                }
                offset += 4;

                chunk.empty = data[offset++] == 1;
                chunk.full = data[offset++] == 1;
                ushort[][] mesh = new ushort[Chunk.lodCount][];
                int len;
                for (int lod = 0; lod < Chunk.lodCount; lod++)
                {
                    len = BitConverter.ToInt32(data, offset);
                    chunk.lengths[lod] = len;
                    mesh[lod] = new ushort[len];
                    offset += 4;

                    for (int i = 0; i < len * 2; i += 2)
                    {
                        mesh[lod][i / 2] = BitConverter.ToUInt16(data, offset + i);
                    }

                    offset += len * 2;
                }

                len = BitConverter.ToInt32(data, offset);
                offset += 4;
                byte[] blocks = new byte[Chunk.Size * Chunk.Size * Chunk.Size];
                Array.Copy(data, offset, blocks, 0, blocks.Length);
                offset += len;

                len = BitConverter.ToInt32(data, offset);
                offset += 4;

                for (int i = 0; i < len; i += 4)
                {
                    Window.Instance.SoundManager.PlaySound((SoundType)data[offset + i + 3], new(data[offset + i] + chunk.X * Chunk.Size,
                        data[offset + i + 1] + chunk.Y * Chunk.Size,
                        data[offset + i + 2] + chunk.Z * Chunk.Size),
                        true);
                }
                offset += len;

                len = BitConverter.ToInt32(data, offset);
                offset += 4;

                for (int i = 0; i < len; i += 4)
                {
                    Window.Instance.ParticleSystem.Emit(
                        data[offset + i] + chunk.X * Chunk.Size,
                        data[offset + i + 1] + chunk.Y * Chunk.Size,
                        data[offset + i + 2] + chunk.Z * Chunk.Size,
                        data[offset + i + 3]);
                }
                offset += len;


                chunk.blocks = blocks;
                chunk.mesh = mesh;

                return true;
            }

            return false;
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

        float showDist = 0;
        float showDistShown = 0;

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

        double pitch = ToRadians(25);

        public double time = 0;
        public int Render(Shader shader, Shader instancedShader, Shader post, Camera camera, double fT, double frameDelta)
        {
            //time += fT * 100000;
            time = 1050 * 1000;

            double t = time / 1000 / 1440 % 1;

            double X = Math.Cos(t * 2 * Math.PI - Math.PI * .5) * Math.Cos(pitch);
            double Y = Math.Sin(t * 2 * Math.PI - Math.PI * .5) * Math.Cos(pitch);
            double Z = Math.Sin(pitch);

            sunPos = new Vector3((float)X, (float)Y, (float)Z);

            //if (showDist > showDistShown)
            //{
            //    showDistShown = (showDistShown * 119 + showDist) / 120;
            //}
            //else
            //{
                showDistShown = (showDistShown * 10 + showDist) / 11;
            //}

            post.SetFloat("renderDistance", Math.Max(0, showDistShown));

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
                Chunk ch = null;
                chunks.TryRemove(removeChunks[i], out ch);
                if (ch == null) continue;

                lock (queueLock)
                {
                    buffers.Enqueue(ch.Buffer);
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

            showDist = 0;

            return RenderWorld(camera.Position, camera.Position, camera.Front, camera.Fov, shader, instancedShader, true, frameDelta);

            //System.Diagnostics.Debug.WriteLine("Verticles renderd: " + verticlesRendered);
        }

        InstancedRenderer renderer;

        public int RenderWorld(Vector3 Position, float radius, Shader shader, Shader instancedShader, double frameDelta)
        {
            int verticlesRendered = 0;
            int chunksRendered = 0;

            Vector3 origin = new Vector3((int)Position.X / Chunk.Size, (int)Position.Y / Chunk.Size, (int)Position.Z / Chunk.Size);
            for (int i = 0; i < chunksLen; i++)
            {
                Vector3 iPos = iterationOrder[i];
                Render(iPos.X + origin.X, iPos.Y + origin.Y, iPos.Z + origin.Z);
            }


            void Render(float x, float y, float z)
            {
                Vector3 v = new Vector3((int)x, (int)y, (int)z);
                if (chunks.ContainsKey(v))
                {
                    Vector3 differenceVector = v - (Position / Chunk.Size);
                    if (chunks[v] != null /*&& differenceVector.Length < radius*/)
                    {
                        Chunk chunk = chunks[v];

                        if (!chunks.ContainsKey(v + new Vector3(0, 1, 0)) ||
                            chunks[v + new Vector3(0, 1, 0)] == null ||
                            !chunks[v + new Vector3(0, 1, 0)].full)
                        {
                            chunksRendered++;
                            int lod = (int)Math.Clamp(differenceVector.Length / size * Chunk.lodCount, 0, Chunk.lodCount);
                            verticlesRendered += chunk.Render(shader, lod);
                        }
                    }
                }
            }

            //if (!gen)
            //{
            player.Render(renderer, frameDelta);
            //}
            foreach (Dictionary<int, DrawableEntity> item in entities.Values)
            {
                foreach (DrawableEntity entity in item.Values)
                {
                    entity.Render(renderer, frameDelta);
                }
            }

            renderer.Render(instancedShader);

            return verticlesRendered;
        }

        public int RenderWorld(Vector3 Position, Vector3 lodPoint, Vector3 Front, float Fov, Shader shader, Shader instancedShader, bool gen, double frameDelta)
        {
            bool allRendered = true, canGen = true;
            int verticlesRendered = 0;
            int chunksRendered = 0;

            Vector3 origin = new Vector3((int)Position.X / Chunk.Size, (int)Position.Y / Chunk.Size, (int)Position.Z / Chunk.Size);
            Vector3 front = Front.Normalized();

            for (int i = 0; i < chunksLen; i++)
            {
                Vector3 iPos = iterationOrder[i];
                Render(iPos.X + origin.X, iPos.Y + origin.Y, iPos.Z + origin.Z);
            }

            void Render(float x, float y, float z)
            {
                Vector3 v = new Vector3((int)x, (int)y, (int)z);
                if (chunks.ContainsKey(v))
                {
                    Vector3 differenceVector = v - (lodPoint / Chunk.Size);
                    if (chunks[v] != null && (!gen || inFov(front, differenceVector, Fov)))
                    {
                        Chunk chunk = chunks[v];
                        
                        if(!chunks.ContainsKey(v + new Vector3(0, 1, 0)) || 
                            chunks[v + new Vector3(0, 1, 0)] == null ||
                            !chunks[v + new Vector3(0, 1, 0)].full)
                        {
                            chunksRendered++;
                            int lod = (int)Math.Clamp(differenceVector.Length / size * Chunk.lodCount, 0, Chunk.lodCount);
                            verticlesRendered += chunk.Render(shader, lod);
                        }

                        if (allRendered)
                            showDist = Math.Max(showDist, Math.Min(Math.Abs((x + .5f) - (int)(lodPoint.X / Chunk.Size)) * Chunk.Size, Math.Abs((z + .5f) - (int)(lodPoint.Z / Chunk.Size)) * Chunk.Size));
                    }
                }
                else
                {
                    allRendered = false;
                    if (gen && canGen)
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
                                    System.Diagnostics.Debug.WriteLine(task.Exception);
                                    Window.exception = "Error generating chunk!";
                                }
                            });
                        }
                        else
                            canGen = false;
                    }
                }
            }

            //if (!gen)
            //{
                player.Render(renderer, frameDelta);
            //}
            foreach (Dictionary<int, DrawableEntity> item in entities.Values)
            {
                foreach (DrawableEntity entity in item.Values)
                {
                    entity.Render(renderer, frameDelta);
                }
            }

            renderer.Render(instancedShader);

            return verticlesRendered;
        }

        private void generate(int x, int y, int z)
        {
            Buffer buf;

            lock (queueLock)
            {
                if(buffers.Count < 1)
                {
                    genDelay--;
                    Window.exception = "Error: not enough buffers!";
                    return;
                }
                buf = buffers.Dequeue();
            }

            Chunk ch = new Chunk(x, y, z, buf, this);

            chunks[new Vector3(x, y, z)] = ch;
            lock (delayLock)
            {
                genDelay--;
            }
        }

        public static int genDelay = 0;

        //private static bool isInView(Vector3 front, Vector3 d1, float radius)
        //{

        //}

        private static bool inFov(Vector3 front, Vector3 d1, double fov)
        {
            double fr = Math.Cos(ToRadians(fov));

            return  DotProduct(front, (d1 + new Vector3(0, 0, 0)).Normalized()) >= fr ||
                    DotProduct(front, (d1 + new Vector3(0, 0, 1)).Normalized()) >= fr ||
                    DotProduct(front, (d1 + new Vector3(0, 1, 0)).Normalized()) >= fr ||
                    DotProduct(front, (d1 + new Vector3(0, 1, 1)).Normalized()) >= fr ||
                    DotProduct(front, (d1 + new Vector3(1, 0, 0)).Normalized()) >= fr ||
                    DotProduct(front, (d1 + new Vector3(1, 0, 1)).Normalized()) >= fr ||
                    DotProduct(front, (d1 + new Vector3(1, 1, 0)).Normalized()) >= fr ||
                    DotProduct(front, (d1 + new Vector3(1, 1, 1)).Normalized()) >= fr;
        }
        private static double DotProduct(Vector3 vec1, Vector3 vec2)
        {
            double tVal = 0;
            tVal += vec1.X * vec2.X;
            tVal += vec1.Y * vec2.Y;
            tVal += vec1.Z * vec2.Z;

            return tVal;
        }
    }
}
