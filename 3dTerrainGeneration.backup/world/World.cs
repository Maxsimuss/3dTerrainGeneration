using _3dTerrainGeneration.audio;
using _3dTerrainGeneration.entity;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    class World
    {
        public static float renderDist = 1024;
        static int size = (int)(renderDist / Chunk.Width * 2);
        int[] VAOs = new int[size * size * 16 + 1];
        int[] VBOs = new int[size * size * 16 + 1];
        public List<Structure> structures = new List<Structure>();
        public List<LohEntity> entities = new List<LohEntity>();
        public Player player;

        public ConcurrentDictionary<Vector2, Chunk> chunks = new ConcurrentDictionary<Vector2, Chunk>();
        object queueLock = new object();
        object delayLock = new object();
        public object structureLock = new object();
        Queue<int> buffers = new Queue<int>();

        Vector3 sunPos = new Vector3(0, 1, 0);
        Vector3 moonPos = new Vector3(0, -1, 0);
        public static Color4 fogColor = Color4.Black;

        public SoundManager soundManager;
        private static Random rnd = new Random();
        public World(Shader lighting)
        {
            player = new Player(this);
            soundManager = new SoundManager();

            GL.GenBuffers(VBOs.Length, VBOs);
            GL.GenVertexArrays(VAOs.Length, VAOs);

            for (int i = 0; i < VBOs.Length; i++)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOs[i]);
                GL.BindVertexArray(VAOs[i]);

                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOs[i]);

                var positionLocation = lighting.GetAttribLocation("aPos");
                GL.EnableVertexAttribArray(positionLocation);
                GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Short, false, 9 * sizeof(short), 0);

                var normalLocation = lighting.GetAttribLocation("aNormal");
                GL.EnableVertexAttribArray(normalLocation);
                GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Short, false, 9 * sizeof(short), 3 * sizeof(short));

                var colorLocaiton = lighting.GetAttribLocation("aColor");
                GL.EnableVertexAttribArray(colorLocaiton);
                GL.VertexAttribPointer(colorLocaiton, 3, VertexAttribPointerType.Short, false, 9 * sizeof(short), 6 * sizeof(short));
                lighting.Use();

                buffers.Enqueue(VBOs[i]);
            }

            lighting.SetVector3("material.specular", new Vector3(1f));
            lighting.SetFloat("material.shininess", 1);
        }

        private Color4 lerp(float sun, float moon, Color4 b, Color4 c, Color4 d)
        {
            float sunAmount = Math.Max(sun, 0);
            float midAmount = Math.Max(1.4f - sun - moon, 0);
            float moonAmount = Math.Max(moon, 0);

            return new Color4(
                b.R * sunAmount + c.R * midAmount + d.R * moonAmount,
                b.G * sunAmount + c.G * midAmount + d.G * moonAmount,
                b.B * sunAmount + c.B * midAmount + d.B * moonAmount,
                b.A * sunAmount + c.A * midAmount + d.A * moonAmount);
        }

        private static double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public void Tick(double fT)
        {
            player.PhisycsUpdate(fT);

            if(entities.Count < 100)
            {
                //entities.Add(new LohEntity(this, meshGenerator, buffers.Dequeue(), player.GetPosition() + new Vector3((float)(rnd.NextDouble() * 256 - 128), -128, (float)(rnd.NextDouble() * 256 - 128))));
            }

            foreach (LohEntity loh in entities)
            {
                loh.PhisycsUpdate(fT);
            }
        }

        float showDist = 0;
        float showDistShown = 0;


        public bool GetBlockAt(double x, double y, double z)
        {
            double chunkX = x / Chunk.Width;
            double chunkZ = z / Chunk.Width;

            Vector2 chunkCoord = new Vector2((int)Math.Floor(chunkX), (int)Math.Floor(chunkZ));
            if (!chunks.ContainsKey(chunkCoord)) return false;

            Chunk chunk = chunks[chunkCoord];
            if (chunk == null) return false;

            x %= Chunk.Width;
            z %= Chunk.Width;

            if (x < 0)
            {
                x += Chunk.Width;
            }

            if (z < 0)
            {
                z += Chunk.Width;
            }

            return chunk.GetBlockAt((int)Math.Floor(x), (int)y, (int)Math.Floor(z));
        }

        Color4 day = new Color4(92 / 255f, 192 / 255f, 214 / 255f, 1);
        Color4 middle = new Color4(222 / 255f, 114 / 255f, 71 / 255f, 1);
        Color4 night = new Color4(5 / 255f, 11 / 255f, 28 / 255f, 1);

        public void Render(Shader shader, Camera camera, double fT)
        {
            soundManager.Update(camera, fT);

            double time = (DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds / 200000) % Math.PI * 2;
            //time = Math.PI / 2;
            double pitch = ToRadians(25);

            double X = Math.Cos(time) * Math.Cos(pitch);
            double Y = Math.Sin(time) * Math.Cos(pitch);
            double Z = Math.Sin(pitch);

            sunPos = new Vector3((float)X, (float)Y, (float)Z);
            moonPos = new Vector3((float)-X, (float)-Y, (float)Z);

            float sunAmt = Math.Max((float)Math.Sin(time) + .2f, 0);
            float moonAmt = Math.Max((float)-Math.Sin(time) + .2f, 0);

            fogColor = lerp(sunAmt, moonAmt, day, middle, night);
            GL.ClearColor(fogColor.R / 2f, fogColor.G / 2f, fogColor.B / 2f, 1f);

            shader.SetVector3("sun.position", sunPos);
            shader.SetFloat("sun.amount", sunAmt);
            shader.SetVector3("sun.ambient", new Vector3(day.R, day.G, day.B) * .25f + new Vector3(.4f));
            shader.SetVector3("sun.diffuse", new Vector3(.75f));
            shader.SetVector3("sun.specular", new Vector3(.03f));

            shader.SetVector3("moon.position", moonPos);
            shader.SetFloat("moon.amount", moonAmt);
            shader.SetVector3("moon.ambient", new Vector3(night.R, night.G, night.B));
            shader.SetVector3("moon.diffuse", new Vector3(night.R, night.G, night.B));
            shader.SetVector3("moon.specular", new Vector3(.02f));

            if (showDist > showDistShown)
            {
                showDistShown = (showDistShown * 119 + showDist) / 120;
            }
            else
            {
                showDistShown = (showDistShown * 10 + showDist) / 11;
            }

            shader.SetFloat("renderDistance", Math.Max(0, showDistShown - 24));

            Vector3 pos = camera.Front.Normalized();

            List<Vector2> removeChunks = new List<Vector2>();
            foreach (var chunk in chunks)
            {
                if (Math.Abs(chunk.Key.X - (int)(camera.Position.X / Chunk.Width)) > size || Math.Abs(chunk.Key.Y - (int)(camera.Position.Z / Chunk.Width)) > size)
                    removeChunks.Add(chunk.Key);
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
            structures.RemoveAll((structure) =>
            {
                return Math.Abs(structure.xPos - (int)camera.Position.X) > renderDist + Chunk.Width * 2 || Math.Abs(structure.zPos - (int)camera.Position.Z) > renderDist + Chunk.Width * 2;
            });

            soundManager.sources.RemoveAll((soundSource) =>
            {
                return Math.Abs(soundSource.position.X - (int)camera.Position.X) > renderDist + Chunk.Width * 2 || Math.Abs(soundSource.position.Z - (int)camera.Position.Z) > renderDist + Chunk.Width * 2;
            });

            showDist = 0;
            bool allRendered = true;

            int chunksRendered = 0;

            int x, z, dx, dy;
            x = z = dx = 0;
            dy = -1;
            int Size = size + 2;
            int t = Size;
            int maxI = t * t;
            for (int i = 0; i < maxI; i++)
            {
                if ((-Size / 2 <= x) && (x <= Size / 2) && (-Size / 2 <= z) && (z <= Size / 2))
                {
                    x += (int)(camera.Position.X / Chunk.Width);
                    z += (int)(camera.Position.Z / Chunk.Width);

                    Vector3 differenceVector = new Vector3(
                            (x + .5f) * Chunk.Width,
                            camera.Position.Y,
                            (z + .5f) * Chunk.Width) - camera.Position;


                    Vector2 v = new Vector2(x, z);
                    if (chunks.ContainsKey(v) && chunks[v] != null)
                    {
                        if (differenceVector.Length < renderDist + 32 && inFov(pos, differenceVector, camera.Fov))
                        {
                            Chunk chunk = chunks[v];

                            double a = Math.Sqrt(differenceVector.Length) * .8 + differenceVector.Length * .2;
                            double b = Math.Sqrt(renderDist + 32) * .8 + (renderDist + 32) * .2;
                            chunk.Render(shader, new Vector3(x * Chunk.Width, 0, z * Chunk.Width), (int)(a / b * Chunk.lodCount));
                            chunksRendered++;
                            if (allRendered)
                                showDist = Math.Max(showDist, Math.Min(Math.Abs((x + .5f) - (int)(camera.Position.X / Chunk.Width)) * Chunk.Width, Math.Abs((z + .5f) - (int)(camera.Position.Z / Chunk.Width)) * Chunk.Width));
                        }
                    }
                    else
                    {
                        allRendered = false;
                        if (genDelay < 1)
                        {
                            chunks[new Vector2(x, z)] = null;

                            int xC = x;
                            int zC = z;

                            lock (delayLock)
                            {
                                genDelay++;
                            }
                            //Task.Run(() =>
                            //{
                            generate(xC, zC);
                            //}).ContinueWith((task) =>
                            //{
                            //    if (task.IsFaulted) Console.WriteLine(task.Exception);
                            //});
                        }
                    }
                }

                x -= (int)(camera.Position.X / Chunk.Width);
                z -= (int)(camera.Position.Z / Chunk.Width);
                if ((x == z) || ((x < 0) && (x == -z)) || ((x > 0) && (x == 1 - z)))
                {
                    t = dx;
                    dx = -dy;
                    dy = t;
                }
                x += dx;
                z += dy;
            }

            genDelay = 0;

            foreach (LohEntity loh in entities)
            {
                loh.Render(shader);
            }

            //Console.WriteLine("Chunks renderd: " + chunksRendered);
        }
        private MeshGenerator meshGenerator = new MeshGenerator();
        private void generate(int x, int z)
        {
            int buf = 0;

            lock (queueLock)
            {
                buf = buffers.Dequeue();
            }

            Chunk ch = new Chunk(x, z, buf, meshGenerator, this);

            chunks[new Vector2(x, z)] = ch;
            lock (delayLock)
            {
                //genDelay--;
            }
        }

        volatile byte genDelay = 0;

        private static bool inFov(Vector3 pos, Vector3 d1, double fov)
        {
            double fr = Math.Cos(ToRadians(fov));

            return DotProduct(pos, (d1 + new Vector3(-Chunk.Width, 0, -Chunk.Width)).Normalized()) >= fr ||
                    DotProduct(pos, (d1 + new Vector3(Chunk.Width, 0, -Chunk.Width)).Normalized()) >= fr ||
                    DotProduct(pos, (d1 + new Vector3(-Chunk.Width, 0, Chunk.Width)).Normalized()) >= fr ||
                    DotProduct(pos, (d1 + new Vector3(Chunk.Width, 0, Chunk.Width)).Normalized()) >= fr;
        }
        private static double DotProduct(Vector3 vec1, Vector3 vec2)
        {
            double tVal = 0;
            tVal += vec1.X * vec2.X;
            tVal += vec1.Z * vec2.Z;

            return tVal;
        }
    }
}
