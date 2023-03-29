using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    public class Chunk
    {
        public static readonly int Size = GameSettings.CHUNK_SIZE;
        public static readonly int LodCount = 3;

        private bool IsMeshReady = false;
        public bool IsRemeshingNeeded = false;
        public bool IsRemeshing = false;
        public uint[][][] MeshData;
        public int[] MeshDataLengths = new int[LodCount];

        public bool HasTerrain = false;
        public bool IsPopulated = false;

        public readonly World world;
        public List<byte> sounds;
        public List<byte> particles;
        public InderectDraw[] drawCall = new InderectDraw[6];

        public int X, Y, Z;

        public VoxelOctree Blocks = new VoxelOctree((int)Math.Log2(Size));

        private int loadedLod = -1;

        private readonly Matrix4x4 modelMatrix;

        public Chunk(World world, int X, int Y, int Z)
        {
            this.world = world;
            this.X = X;
            this.Y = Y;
            this.Z = Z;

            modelMatrix = Matrix4x4.CreateTranslation(X * Size, Y * Size, Z * Size);

            if (ChunkIO.Load(this)) return;

            sounds = new List<byte>();
            particles = new List<byte>();

            Mesh();

            ChunkIO.Save(this);
        }

        public void Mesh()
        {
            if (IsRemeshing)
            {
                return;
            }
            IsRemeshing = true;
            IsRemeshingNeeded = false;

            Task.Run(() =>
            {
                MeshData = new uint[LodCount][][];

                for (int i = 0; i < LodCount; i++)
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
                                byte bl = Blocks.GetValue(x * lod, y, z * lod);
                                if (bl != 0)
                                {
                                    data.SetBlockUnsafe(x, y, z, Materials.Get((byte)(bl - 1)));
                                }
                            }
                        }
                    }

                    MeshData[i] = data.Mesh(0, lod);
                    MeshDataLengths[i] = MeshData[i].Length;
                }

                loadedLod = -1;
                IsRemeshing = false;
                IsMeshReady = true;
            });
        }

        public bool GetBlockAt(int x, int y, int z)
        {
            return Blocks.GetValue(x, y, z) != 0;
        }

        public int Render(int lod, bool ortho)
        {
            if(IsRemeshingNeeded)
            {
                Mesh();
            }

            if(!IsMeshReady)
            {
                return 0;
            }

            lod = Math.Min(LodCount - 1, lod);

            if (lod != loadedLod && !IsRemeshing)
            {
                for (int i = 0; i < 6; i++)
                {
                    drawCall[i] = GameRenderer.Instance.SubmitMesh(MeshData[lod][i], drawCall[i]);
                }
                loadedLod = lod;
            }

            if (drawCall[0] == null)
            {
                return 0;
            }

            if (ortho)
            {
                Vector3 dir = world.SunPosition;
                if (Vector3.Dot(dir, new Vector3(0, 0, 1)) < 0)
                {
                    GameRenderer.Instance.QueueRender(drawCall[5], modelMatrix);
                }
                else
                {
                    GameRenderer.Instance.QueueRender(drawCall[2], modelMatrix);
                }

                if (Vector3.Dot(dir, new Vector3(0, 1, 0)) < 0)
                {
                    GameRenderer.Instance.QueueRender(drawCall[4], modelMatrix);
                }
                else
                {
                    GameRenderer.Instance.QueueRender(drawCall[1], modelMatrix);
                }

                if (Vector3.Dot(dir, new Vector3(1, 0, 0)) < 0)
                {
                    GameRenderer.Instance.QueueRender(drawCall[3], modelMatrix);
                }
                else
                {
                    GameRenderer.Instance.QueueRender(drawCall[0], modelMatrix);
                }
            }
            else
            {
                GameRenderer.Instance.QueueRender(drawCall[0], modelMatrix);
                GameRenderer.Instance.QueueRender(drawCall[1], modelMatrix);
                GameRenderer.Instance.QueueRender(drawCall[2], modelMatrix);
                GameRenderer.Instance.QueueRender(drawCall[3], modelMatrix);
                GameRenderer.Instance.QueueRender(drawCall[4], modelMatrix);
                GameRenderer.Instance.QueueRender(drawCall[5], modelMatrix);
            }

            return MeshDataLengths[lod];
        }
    }
}
