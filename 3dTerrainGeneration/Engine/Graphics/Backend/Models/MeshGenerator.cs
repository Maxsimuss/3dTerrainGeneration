using _3dTerrainGeneration.Game.GameWorld;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Formats.Asn1.AsnWriter;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models
{
    class MeshGenerator
    {
        [StructLayout(LayoutKind.Sequential)]
        unsafe struct NativeMeshData
        {
            public VertexData** Vertices;
            public int* Lengths;
        }

        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern NativeMeshData* MeshLODs(IntPtr svo, int chunkSize, int lodCount);

        public static VertexData[][][] MeshLODs(Chunk chunk)
        {
            VertexData[][][] result = new VertexData[Chunk.LOD_COUNT][][];

            unsafe
            {
                NativeMeshData* nativeMeshData = MeshLODs(chunk.Blocks.Handle, Chunk.CHUNK_SIZE, Chunk.LOD_COUNT);

                for (int lod = 0; lod < Chunk.LOD_COUNT; lod++)
                {
                    result[lod] = new VertexData[6][];
                    for (int i = 0; i < 6; i++)
                    {
                        int len = nativeMeshData[lod].Lengths[i];

                        result[lod][i] = new VertexData[len];

                        fixed (VertexData* resultPtr = result[lod][i])
                        {
                            byte* src = (byte*)nativeMeshData[lod].Vertices[i];
                            byte* dst = (byte*)resultPtr;

                            for (int j = 0; j < len; j++)
                            {
                                result[lod][i][j] = nativeMeshData[lod].Vertices[i][j];

                                //dst[j] = src[j];

                                //Vector128<byte> vector = Sse2.LoadVector128( + j * VertexData.Size);
                                //Sse2.Store( + j * VertexData.Size, vector);
                            }
                        }
                    }
                }
            }

            return result;
        }

        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static unsafe extern NativeMeshData GreedyMesh(uint* blocks, int width, int height, short scale, byte emission);

        public static VertexData[][] GenerateMeshFromBlocks(Model meshData, int width, int height, byte emission, int scale = 1)
        {
            VertexData[][] result = new VertexData[6][];

            unsafe
            {
                fixed (uint* blocks = meshData.blocks)
                {
                    NativeMeshData nativeMeshData = GreedyMesh(blocks, width, height, (short)scale, (byte)emission);

                    for (int i = 0; i < 6; i++)
                    {
                        int len = nativeMeshData.Lengths[i];

                        result[i] = new VertexData[len];

                        fixed (VertexData* resultPtr = result[i])
                        {
                            byte* src = (byte*)nativeMeshData.Vertices[i];
                            byte* dst = (byte*)resultPtr;

                            for (int j = 0; j < len; j++)
                            {
                                result[i][j] = nativeMeshData.Vertices[i][j];

                                //dst[j] = src[j];

                                //Vector128<byte> vector = Sse2.LoadVector128( + j * VertexData.Size);
                                //Sse2.Store( + j * VertexData.Size, vector);
                            }
                        }
                    }
                }
            }

            //List<VertexData>[] quads = new List<VertexData>[6] { new(), new(), new(), new(), new(), new() };

            //GreedyMesh(quads, meshData.blocks, width, height, (short)scale, emission);

            //for (int i = 0; i < 6; i++)
            //{
            //    result[i] = quads[i].ToArray();
            //}

            return result;
        }
    }
}
