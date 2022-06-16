using _3dTerrainGeneration.audio;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    class Chunk
    {
        public static int Width = 16;
        public static int Height = 128;

        public short[][] mesh;
        public byte[][][] blocks = new byte[Width][][];
        public static int lodCount = 3;

        public int X, Z, Buffer;
        private int loadedLod = -1;

        public Chunk(int X, int Z, int Buffer, MeshGenerator meshGen, World world)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            Random rnd = new Random(X + Z * Width);
            this.X = X;
            this.Z = Z;
            this.Buffer = Buffer;

            for (int x = 0; x < Width; x++)
            {
                blocks[x] = new byte[Width][];
                for (int z = 0; z < Width; z++)
                {
                    blocks[x][z] = new byte[Height];
                }
            }

            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Width; z++)
                {
                    double h = meshGen.GetHeight(X * Width + x, Z * Width + z);
                    for (int y = 0; y < h && y < Height; y++)
                    {
                        if (h - y < y / 16 - rnd.NextDouble() * 4 - 6)
                            blocks[x][z][y] = MaterialType.SNOW;
                        else if (h - y < 2)
                            blocks[x][z][y] = MaterialType.GRASS;
                        else
                            blocks[x][z][y] = MaterialType.STONE;
                    }

                    if(h < 64 && rnd.NextDouble() > .998 + meshGen.noise.GetNoise((x + X * Width) / 2 + 1337, (z + Z * Width) / 2 + 420) * .005)
                    {
                        Structure structure = null;
                        switch (rnd.Next(0, 3))
                        {
                            case 0:
                                structure = new Pine(X * Width + x, (int)h, Z * Width + z);
                                break;
                            case 1:
                                structure = new Sakura(X * Width + x, (int)h, Z * Width + z);
                                break;
                            case 2:
                                structure = new Maple(X * Width + x, (int)h, Z * Width + z);
                                break;
                        }
                        world.soundManager.PlaySound(SoundType.FOREST, new Vector3(X * Width + x, (float)h, Z * Width + z), true);
                        
                        lock (world.structureLock)
                        {
                            world.structures.Add(structure);
                        }

                        foreach (Chunk chunk in world.chunks.Values)
                        {
                            if (chunk != null && structure.Spawn(ref chunk.blocks, chunk.X * Width, chunk.Z * Width))
                            {
                                chunk.Mesh(meshGen);
                            }
                        }
                    }
                }
            }


            List<Structure> remove = new List<Structure>();
            lock (world.structureLock)
            {
                foreach (Structure st in world.structures)
                {
                    st.Spawn(ref blocks, X * Width, Z * Width);
                    if (st.blocks <= 0)
                    {
                        remove.Add(st);
                    }
                }
            }
            foreach (Structure st in remove)
            {
                lock (world.structureLock)
                {
                    world.structures.Remove(st);
                }
                //Console.WriteLine("removed structure, left {0}", world.structures.Count);
            }

            Mesh(meshGen);

            //Console.WriteLine("time elapsed: " + sw.ElapsedMilliseconds);
            //sw.Stop();
        }

        public void Mesh(MeshGenerator meshGen)
        {
            mesh = new short[lodCount][];
            for (int i = 0; i < lodCount; i++)
            {
                short lod = (short)Math.Pow(2, i);
                int wl = Width / lod;
                byte[][][] copy = new byte[wl][][];

                bool[][] usedBlocks = new bool[Height / Width][];
                for (int j = 0; j < Height / Width; j++)
                {
                    usedBlocks[j] = new bool[meshGen.materials.materials.Count];
                }

                for (short x = 0; x < wl; x++)
                {
                    copy[x] = new byte[wl][];

                    for (short z = 0; z < wl; z++)
                    {
                        copy[x][z] = new byte[Height];
                        for (short y = 0; y < Height; y++)
                        {
                            for (short i1 = 0; i1 < lod; i1++)
                            {
                                for (short i2 = 0; i2 < lod; i2++)
                                {
                                    if (copy[x][z][y] == 0)
                                    {
                                        byte bl = blocks[x * lod + i1][z * lod + i2][y];
                                        if (bl != 0)
                                        {
                                            copy[x][z][y] = bl;
                                            usedBlocks[y / (Width)][bl - 1] = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                short[] m = meshGen.GenerateMeshFromBlocks(copy, Width, Height, lod, usedBlocks);
                lengths[i] = m.Length;
                mesh[i] = m;
            }

            loadedLod = -1;
        }

        int[] lengths = new int[lodCount];

        public bool GetBlockAt(int x, int y, int z)
        {
            if (y >= Height) return false;
            if (y < 0) return true;

            return blocks[x % Chunk.Width][z % Chunk.Width][y] != 0;
        }

        public void Render(Shader lighting, Vector3 pos, int lod)
        {
            lod = Math.Min(lodCount - 1, lod);

            int cubeLenght = lengths[lod];

            GL.BindVertexArray(Buffer);
            if(lod != loadedLod)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, Buffer);
                GL.BufferData(BufferTarget.ArrayBuffer, 0, (IntPtr)0, BufferUsageHint.DynamicDraw);
                GL.BufferData(BufferTarget.ArrayBuffer, cubeLenght * sizeof(short), mesh[lod], BufferUsageHint.DynamicDraw);
                loadedLod = lod;
            }

            lighting.SetMatrix4("model", Matrix4.Identity * Matrix4.CreateTranslation(pos));
            GL.DrawArrays(PrimitiveType.Quads, 0, cubeLenght);
        }
    }
}
