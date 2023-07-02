using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Util;
using System;
using System.Numerics;

namespace _3dTerrainGeneration.Game.GameWorld
{
    [Flags]
    internal enum ChunkState : byte
    {
        AwaitingTerrainGeneration = 1,
        AwaitingTerrainPopulation = 1 << 1,
        AwaitingMeshGeneration = 1 << 2,
        NeedsRemeshing = 1 << 3,
        NeedsUploading = 1 << 4,
        HasTerrain = 1 << 5,
        IsPopulated = 1 << 6,
    }

    internal class Chunk
    {
        public static readonly int CHUNK_SIZE = 128;
        public static readonly int LOD_COUNT = 5;

        public readonly int X, Y, Z;
        public Vector3I Position => new Vector3I(X, Y, Z);
        public World World;
        public VoxelOctree Blocks = new VoxelOctree((int)Math.Log2(CHUNK_SIZE));

        public ChunkState State;
        private int currentLod;
        private readonly Matrix4x4 modelMatrix;
        private InderectDraw[] drawCalls = new InderectDraw[LOD_COUNT * 6];
        private VertexData[][][] meshData = new VertexData[LOD_COUNT][][];

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
            State &= ~ChunkState.NeedsRemeshing;

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

                lock (meshData)
                {
                    meshData[i] = data.Mesh(0, lod);
                }
            }

            State |= ChunkState.NeedsUploading;
        }

        public uint GetBlockAt(int x, int y, int z)
        {
            return Blocks.GetValue(x, y, z);
        }

        public void Unload()
        {
            for (int i = 0; i < LOD_COUNT; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    if (drawCalls[i * 6 + j] != null)
                    {
                        SceneRenderer.Instance.FreeMemory(drawCalls[i * 6 + j]);
                    }
                }
            }

            State |= ChunkState.NeedsUploading;
        }

        public void SetLOD(int lod)
        {
            currentLod = lod;
        }

        public void Render(bool ortho, Vector3 viewDirection)
        {
            if ((State & ChunkState.NeedsUploading) != 0)
            {
                State &= ~ChunkState.NeedsUploading;

                for (int i = 0; i < LOD_COUNT; i++)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        lock (meshData)
                        {
                            drawCalls[i * 6 + j] = SceneRenderer.Instance.SubmitMesh(meshData[i][j], drawCalls[i * 6 + j]);
                        }
                    }
                }
            }

            if (drawCalls[0] == null)
            {
                return;
            }

            if (ortho)
            {
                if (Vector3.Dot(viewDirection, new Vector3(0, 0, 1)) < 0)
                {
                    SceneRenderer.Instance.QueueRender(drawCalls[currentLod * 6 + 5], modelMatrix);
                }
                else
                {
                    SceneRenderer.Instance.QueueRender(drawCalls[currentLod * 6 + 2], modelMatrix);
                }

                if (Vector3.Dot(viewDirection, new Vector3(0, 1, 0)) < 0)
                {
                    SceneRenderer.Instance.QueueRender(drawCalls[currentLod * 6 + 4], modelMatrix);
                }
                else
                {
                    SceneRenderer.Instance.QueueRender(drawCalls[currentLod * 6 + 1], modelMatrix);
                }

                if (Vector3.Dot(viewDirection, new Vector3(1, 0, 0)) < 0)
                {
                    SceneRenderer.Instance.QueueRender(drawCalls[currentLod * 6 + 3], modelMatrix);
                }
                else
                {
                    SceneRenderer.Instance.QueueRender(drawCalls[currentLod * 6 + 0], modelMatrix);
                }
            }
            else
            {
                SceneRenderer.Instance.QueueRender(drawCalls[currentLod * 6 + 0], modelMatrix);
                SceneRenderer.Instance.QueueRender(drawCalls[currentLod * 6 + 1], modelMatrix);
                SceneRenderer.Instance.QueueRender(drawCalls[currentLod * 6 + 2], modelMatrix);
                SceneRenderer.Instance.QueueRender(drawCalls[currentLod * 6 + 3], modelMatrix);
                SceneRenderer.Instance.QueueRender(drawCalls[currentLod * 6 + 4], modelMatrix);
                SceneRenderer.Instance.QueueRender(drawCalls[currentLod * 6 + 5], modelMatrix);
            }
        }
    }
}
