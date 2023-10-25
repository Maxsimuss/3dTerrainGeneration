using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.Graphics.Backend.Models;
using _3dTerrainGeneration.Engine.Util;
using OpenTK.Windowing.GraphicsLibraryFramework;
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
        public static readonly int LOD_COUNT = 4;

        public readonly int X, Y, Z;
        public Vector3I Position => new Vector3I(X, Y, Z);
        public World World;
        public VoxelOctree Blocks = new VoxelOctree((int)Math.Log2(CHUNK_SIZE));

        public ChunkState State;
        private int neededLod = LOD_COUNT - 1, loadedLod;
        private readonly Matrix4x4 modelMatrix;
        private InderectDraw[] drawCalls = new InderectDraw[6];
        private VertexData[][][] meshData = new VertexData[LOD_COUNT][][];

        public Chunk(World world, int X, int Y, int Z)
        {
            this.World = world;
            this.X = X;
            this.Y = Y;
            this.Z = Z;

            modelMatrix = Matrix4x4.CreateTranslation(X * CHUNK_SIZE, Y * CHUNK_SIZE, Z * CHUNK_SIZE);
        }

        public void Mesh()
        {
            State &= ~ChunkState.NeedsRemeshing;

            VertexData[][][] tempMesh = MeshGenerator.MeshLODs(this);

            lock (meshData)
            {
                meshData = tempMesh;
            }

            State |= ChunkState.NeedsUploading;
        }

        public uint GetBlockAt(int x, int y, int z)
        {
            return Blocks.GetValue(x, y, z);
        }

        public void Unload()
        {
            return;

            for (int j = 0; j < 6; j++)
            {
                if (drawCalls[j] != null)
                {
                    SceneRenderer.Instance.FreeMemory(drawCalls[j]);
                }
            }

            State |= ChunkState.NeedsUploading;
        }

        public void SetLOD(int lod)
        {
            if(lod < 0 || lod >= LOD_COUNT)
            {
                throw new ArgumentException("LOD is out of range!");
            }

            neededLod = lod;
        }

        public void Render(bool ortho, Vector3 viewDirection)
        {
            lock (meshData)
            {
                if (meshData[neededLod] != null && ((State & ChunkState.NeedsUploading) != 0 || loadedLod != neededLod))
                {
                    State &= ~ChunkState.NeedsUploading;

                    for (int j = 0; j < 6; j++)
                    {
                        drawCalls[j] = SceneRenderer.Instance.SubmitMesh(meshData[neededLod][j], drawCalls[j]);
                    }

                    loadedLod = neededLod;
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
        }
    }
}
