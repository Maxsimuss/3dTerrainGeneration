using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Util;
using System;
using System.Diagnostics;
using System.Numerics;

namespace _3dTerrainGeneration.Game.GameWorld
{
    internal class Chunk
    {
        public static readonly int CHUNK_SIZE = 128;
        public static readonly int LOD_COUNT = 4;

        private volatile bool isMeshReady = false;
        private int loadedLod = -1;

        private readonly Matrix4x4 modelMatrix;
        private InderectDraw[] drawCalls = new InderectDraw[6];
        private volatile VertexData[][][] meshData;
        private volatile int[] meshDataLengths = new int[LOD_COUNT];

        public volatile bool IsRemeshingNeeded = false;
        public volatile bool IsRemeshing = false;
        public volatile bool HasTerrain = false;
        public volatile bool IsPopulated = false;

        public readonly int X, Y, Z;
        public Vector3I Position => new Vector3I(X, Y, Z);

        public World World;
        public VoxelOctree Blocks = new VoxelOctree((int)Math.Log2(CHUNK_SIZE));


        public Chunk(World world, int X, int Y, int Z)
        {
            this.World = world;
            this.X = X;
            this.Y = Y;
            this.Z = Z;

            modelMatrix = Matrix4x4.CreateTranslation(X * CHUNK_SIZE, Y * CHUNK_SIZE, Z * CHUNK_SIZE);

            if (ChunkIO.Load(this)) return;

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

            meshData = new VertexData[LOD_COUNT][][];
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();
            for (int i = 0; i < LOD_COUNT; i++)
            {

                short lod = (short)Math.Pow(2, i);
                int wl = CHUNK_SIZE / lod;

                Model data = new Model();
                data.SetDimensions(wl, CHUNK_SIZE);

                for (short x = 0; x < wl; x++)
                {
                    for (short z = 0; z < wl; z++)
                    {
                        for (short y = 0; y < CHUNK_SIZE; y++)
                        {
                            uint bl = Blocks.GetValue(x * lod, y, z * lod);
                            if (bl != 0)
                            {
                                data.SetBlockUnsafe(x, y, z, bl);
                            }
                        }
                    }
                }

                meshData[i] = data.Mesh(0, lod);
                meshDataLengths[i] = meshData[i].Length;
            }
            Console.WriteLine(sw.ElapsedMilliseconds);

            loadedLod = -1;
            IsRemeshing = false;
            isMeshReady = true;
        }

        public uint GetBlockAt(int x, int y, int z)
        {
            return Blocks.GetValue(x, y, z);
        }

        public int Render(int lod, bool ortho, Vector3 viewDirection)
        {
            if (!isMeshReady)
            {
                return 0;
            }

            lod = Math.Min(LOD_COUNT - 1, lod);

            if (lod != loadedLod && !IsRemeshing)
            {
                for (int i = 0; i < 6; i++)
                {
                    drawCalls[i] = SceneRenderer.Instance.SubmitMesh(meshData[lod][i], drawCalls[i]);
                }
                loadedLod = lod;
            }

            if (drawCalls[0] == null)
            {
                return 0;
            }

            if (ortho)
            {
                if (Vector3.Dot(viewDirection, new Vector3(0, 0, 1)) < 0)
                {
                    SceneRenderer.Instance.QueueRender(drawCalls[5], modelMatrix);
                }
                else
                {
                    SceneRenderer.Instance.QueueRender(drawCalls[2], modelMatrix);
                }

                if (Vector3.Dot(viewDirection, new Vector3(0, 1, 0)) < 0)
                {
                    SceneRenderer.Instance.QueueRender(drawCalls[4], modelMatrix);
                }
                else
                {
                    SceneRenderer.Instance.QueueRender(drawCalls[1], modelMatrix);
                }

                if (Vector3.Dot(viewDirection, new Vector3(1, 0, 0)) < 0)
                {
                    SceneRenderer.Instance.QueueRender(drawCalls[3], modelMatrix);
                }
                else
                {
                    SceneRenderer.Instance.QueueRender(drawCalls[0], modelMatrix);
                }
            }
            else
            {
                SceneRenderer.Instance.QueueRender(drawCalls[0], modelMatrix);
                SceneRenderer.Instance.QueueRender(drawCalls[1], modelMatrix);
                SceneRenderer.Instance.QueueRender(drawCalls[2], modelMatrix);
                SceneRenderer.Instance.QueueRender(drawCalls[3], modelMatrix);
                SceneRenderer.Instance.QueueRender(drawCalls[4], modelMatrix);
                SceneRenderer.Instance.QueueRender(drawCalls[5], modelMatrix);
            }

            return meshDataLengths[lod];
        }
    }
}
