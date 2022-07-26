using _3dTerrainGeneration.audio;
using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.util;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{

    public class Chunk
    {
        static LibNoise.Primitive.BevinsValue bevins = new LibNoise.Primitive.BevinsValue();
        static LibNoise.Primitive.SimplexPerlin perlin = new LibNoise.Primitive.SimplexPerlin();

        public static int Size = GameSettings.CHUNK_SIZE;

        public ushort[][] mesh;
        public int[] lengths = new int[lodCount];
        public byte[] blocks = null;
        public object dataLock = new object();
        public object meshLock = new object();
        public static int lodCount = 3;

        public int X, Y, Z;
        private int loadedLod = -1;
        public bool empty = true;
        public bool full = true;
        public List<byte> sounds;
        public List<byte> particles;
        public InderectDraw mem;
        private Matrix4 modelMatrix;

        public static float GetPerlin(float x, float z, float scale)
        {
            return perlin.GetValue(x * scale, z * scale) / 2 + .5f;
        }
        public static float GetPerlin(float x, float scale)
        {
            return perlin.GetValue(x * scale);
        }

        public static float GetNoise(float x, float y, float z, float scale)
        {
            return smoothstep(0, 1, bevins.GetValue(x * scale, y * scale, z * scale) / 2 + .5f);
        }

        public static float smoothstep(float edge0, float edge1, float x)
        {
            // Scale, bias and saturate x to 0..1 range
            x = clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
            // Evaluate polynomial
            return x * x * (3 - 2 * x);
        }

        public static float clamp(float x, float lowerlimit, float upperlimit)
        {
            if (x < lowerlimit)
                x = lowerlimit;
            if (x > upperlimit)
                x = upperlimit;
            return x;
        }

        public static float OcataveNoise(float x, float z, float scale, int octaves)
        {
            float noise = 0;
            for (int i = 1; i < octaves; i++)
            {
                noise += GetPerlin(x, z, scale * i * i * i) / i / i / i;
            }

            return noise;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetValue(byte[] arr, int x, int z, int y)
        {
            return arr[(x % Size * Size + z % Size) * Size + y % Size];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(byte[] arr, int x, int z, int y, byte val)
        {
            arr[(x * Size + z) * Size + y] = val;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static byte GetValue(Octree arr, int x, int z, int y)
        //{
        //    return arr.GetValue((byte)x, (byte)y, (byte)z);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void SetValue(Octree arr, int x, int z, int y, byte val)
        //{
        //    arr.SetValue((byte)x, (byte)y, (byte)z, val);
        //}

        public Chunk(int X, int Y, int Z)
        {
            Random rnd = new Random((X * Size + Y) * Size + Z);
            this.X = X;
            this.Y = Y;
            this.Z = Z;

            modelMatrix = Matrix4.CreateTranslation(X * Size, Y * Size, Z * Size);

            if (World.Load(this)) return;

            sounds = new List<byte>();
            particles = new List<byte>();

            for (int _x = 0; _x < Size; _x++)
            {
                int x = _x + X * Size;
                for (int _z = 0; _z < Size; _z++)
                {
                    int z = _z + Z * Size;
                    int yO = Y * Size;
                    float h = GetHeight(x, z);

                    for (int i = 0; i < Size; i++)
                    {
                        int y = yO + i;

                        if (y < h)
                        {
                            SetBlock(x, y, z, Materials.IdOf(0x877F6C));
                        }
                        else if (y - 1 < h)
                        {

                            float temp = smoothstep(0, 1, GetPerlin(x, z, .00025f) - (y / 512f) + rnd.NextSingle() * .05f - .025f);
                            if (temp < .16)
                            {
                                SetBlock(x, y, z, Materials.IdOf(255, 255, 255));
                            }
                            else
                            {
                                temp -= .16f;
                                temp /= 1f - .16f;

                                float humidity = smoothstep(0, 1, GetPerlin(x + 12312, z - 124124, .00025f) + rnd.NextSingle() * .05f - .025f);

                                //if(temp > .85 && humidity < .3 && rnd.NextSingle() < .001)
                                //{
                                //    ImportedStructure str = new ImportedStructure("trees/cactus0/cactus0.vox", x, y + 1, z);
                                //    str.Spawn(ref blocks, ref dataLock, X * Size, Y * Size, Z * Size);
                                //}

                                SetBlock(x, y, z, Materials.IdOf(
                                    Color.HsvToRgb(
                                        150 - (byte)((byte)(temp * 8) * 13),
                                        166 + (byte)((byte)(humidity * 4) * 16),
                                        220 - (byte)((byte)(humidity * 4) * 15)
                                    )
                                ));
                            }
                        }
                    }
                }
            }

            //lock (world.structureLock)
            //{
            //    foreach (var s in world.structures)
            //    {
            //        if (s.Spawn(ref blocks, ref dataLock, X * Size, Y * Size, Z * Size))
            //        {
            //            empty = false;
            //        }
            //    }
            //}

            Mesh();

            World.Save(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetBlock(int x, int y, int z, byte type)
        {
            if (empty)
            {
                blocks = new byte[Size * Size * Size];
                empty = false;
            }
            SetValue(blocks, x - X * Size, z - Z * Size, y - Y * Size, type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetHeight(int x, int z)
        {
            return (float)Math.Pow(OcataveNoise(x, z, .0005f / 4, 8) * 1.2, 7) * GetPerlin(x, z, .0005f / 4) * 255 * 4;
        }

        public void Mesh()
        {
            if (empty)
            {
                full = false;
                return;
            }
            mesh = new ushort[lodCount][];

            for (int i = 0; i < lodCount; i++)
            {
                short lod = (short)Math.Pow(2, i);
                int wl = Size / lod;

                MeshData data = new MeshData();
                data.SetDimensions(wl, Size);

                for (short x = 0; x < wl; x++)
                {
                    for (short z = 0; z < wl; z++)
                    {
                        for (short y = 0; y < Size; y++)
                        {
                            byte bl = GetValue(blocks, x * lod, z * lod, y);
                            if (bl != 0)
                            {
                                data.SetBlockUnsafe(x, y, z, Materials.pallette[bl - 1]);
                            }
                            else
                            {
                                full = false;
                            }
                        }
                    }
                }


                mesh[i] = data.Mesh(0, lod);
                lengths[i] = mesh[i].Length;
                if(i == loadedLod)
                {
                    loadedLod = -1;
                }
            }
        }

        public bool GetBlockAt(int x, int y, int z)
        {
            return !empty && GetValue(blocks, x, z, y) != 0;
        }

        public int Render(int lod)
        {
            if (empty || full) return 0;
            
            lod = Math.Min(lodCount - 1, lod);

            int cubeLenght = lengths[lod];
            if (lod != loadedLod)
            {
                mem = World.gameRenderer.SubmitMesh(mesh[lod], mem);
                loadedLod = lod;
            }

            World.gameRenderer.QueueRender(mem, modelMatrix);

            return cubeLenght;
        }
    }
}
