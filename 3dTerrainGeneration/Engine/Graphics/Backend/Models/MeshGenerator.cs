//#define QUICK_MESH

using _3dTerrainGeneration.Engine.Util;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models
{
    class MeshGenerator
    {
#if QUICK_MESH
        public static void GreedyMesh(List<VertexData>[] quads, byte[] blocks, int Width, int Height, short scale, List<uint> colors, byte emission)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Width; z++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        byte id = blocks[(x * Width + z) * Height + y];
                        if (id != 0)
                        {
                            uint color = colors[id - 1];
                            byte r = (byte)(color >> 16 & 0xFF);
                            byte g = (byte)(color >> 8 & 0xFF);
                            byte b = (byte)(color & 0xFF);

                            if ((y + 1 == Height || blocks[(x * Width + z) * Height + y + 1] == 0))
                            {
                                Face(x, y + 1, z, 1);
                            }
                            if ((x + 1 == Width || blocks[((x + 1) * Width + z) * Height + y] == 0))
                            {
                                Face(x + 1, y, z, 0);
                            }
                            if ((z + 1 == Width || blocks[(x * Width + z + 1) * Height + y] == 0))
                            {
                                Face(x, y, z + 1, 2);
                            }

                            if ((y == 0 || blocks[(x * Width + z) * Height + y - 1] == 0))
                            {
                                Face(x, y, z, 4);
                            }
                            if ((x == 0 || blocks[((x - 1) * Width + z) * Height + y] == 0))
                            {
                                Face(x, y, z, 3);
                            }
                            if ((z == 0 || blocks[(x * Width + z - 1) * Height + y] == 0))
                            {
                                Face(x, y, z, 5);
                            }

                            void Face(int x, int y, int z, int face)
                            {
                                List<VertexData> quad = quads[face];
                                switch (face)
                                {
                                    case 0:
                                        AddPoint(x, y, z);
                                        AddPoint(x, y + 1, z);
                                        AddPoint(x, y + 1, z + 1);
                                        AddPoint(x, y + 1, z + 1);
                                        AddPoint(x, y, z + 1);
                                        AddPoint(x, y, z);

                                        break;
                                    case 1:
                                        AddPoint(x + 1, y, z + 1);
                                        AddPoint(x + 1, y, z);
                                        AddPoint(x, y, z);
                                        AddPoint(x, y, z);
                                        AddPoint(x, y, z + 1);
                                        AddPoint(x + 1, y, z + 1);
                                        break;
                                    case 2:
                                        AddPoint(x, y, z);
                                        AddPoint(x + 1, y, z);
                                        AddPoint(x + 1, y + 1, z);
                                        AddPoint(x + 1, y + 1, z);
                                        AddPoint(x, y + 1, z);
                                        AddPoint(x, y, z);
                                        break;
                                    case 3:
                                        AddPoint(x, y + 1, z + 1);
                                        AddPoint(x, y + 1, z);
                                        AddPoint(x, y, z);
                                        AddPoint(x, y, z);
                                        AddPoint(x, y, z + 1);
                                        AddPoint(x, y + 1, z + 1);

                                        break;
                                    case 4:
                                        AddPoint(x, y, z);
                                        AddPoint(x + 1, y, z);
                                        AddPoint(x + 1, y, z + 1);
                                        AddPoint(x + 1, y, z + 1);
                                        AddPoint(x, y, z + 1);
                                        AddPoint(x, y, z);
                                        break;
                                    case 5:
                                        AddPoint(x + 1, y + 1, z);
                                        AddPoint(x + 1, y, z);
                                        AddPoint(x, y, z);
                                        AddPoint(x, y, z);
                                        AddPoint(x, y + 1, z);
                                        AddPoint(x + 1, y + 1, z);
                                        break;
                                }
                                void AddPoint(int x, int y, int z)
                                {
                                    quad.Add(new VertexData(x * scale, y, z * scale, face, r, g, b));

                                    //quad.Add((uint)((byte)(x * scale) << 25 | (byte)(y) << 18 | (byte)(z * scale) << 11 | ((byte)face) << 8 |
                                    //    (((byte)(r / 36)) & (byte)7) << 5 | (((byte)(g / 36)) & (byte)7) << 2 | (((byte)(b / 85)) & 0x03)));
                                }
                            }
                        }
                    }
                }
            }
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void GreedyMesh(List<VertexData>[] quads, uint[] blocks, int Width, int Height, short scale, byte emission)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            uint GetBlock(Vector3I blockPosition)
            {
                if (blockPosition.X >= Width || blockPosition.Z >= Width || blockPosition.Y >= Height ||
                    blockPosition.X < 0 || blockPosition.Z < 0 || blockPosition.Y < 0)
                {
                    return 0;
                }

                return blocks[(blockPosition.X * Width + blockPosition.Z) * Height + blockPosition.Y];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool IsBlockFaceVisible(Vector3I blockPosition, int axis, bool backFace)
            {
                blockPosition[axis] += backFace ? -1 : 1;
                return GetBlock(blockPosition) == 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool CompareStep(Vector3I a, Vector3I b, int direction, bool backFace)
            {
                uint blockA = GetBlock(a);
                uint blockB = GetBlock(b);

                return blockA == blockB && blockB != 0 && IsBlockFaceVisible(b, direction, backFace);
            }

            Vector3I Dimensions = new Vector3I(Width, Height, Width);
            bool[,] merged;

            Vector3I startPos, currPos, quadSize, m, n, offsetPos;
            Vector3I[] vertices;

            uint startBlock;
            int direction, workAxis1, workAxis2;

            // Iterate over each face of the blocks.
            for (int face = 0; face < 6; face++)
            {
                bool isBackFace = face > 2;
                direction = face % 3;
                byte nx = (byte)(direction == 0 ? isBackFace ? 0 : 14 : 7);
                byte ny = (byte)(direction == 1 ? isBackFace ? 0 : 14 : 7);
                byte nz = (byte)(direction == 2 ? isBackFace ? 0 : 14 : 7);

                workAxis1 = (direction + 1) % 3;
                workAxis2 = (direction + 2) % 3;

                startPos = new Vector3I();
                currPos = new Vector3I();

                // Iterate over the chunk layer by layer.
                for (startPos[direction] = 0; startPos[direction] < Dimensions[direction]; startPos[direction]++)
                {
                    merged = new bool[Dimensions[workAxis1], Dimensions[workAxis2]];

                    // Build the slices of the mesh.
                    for (startPos[workAxis1] = 0; startPos[workAxis1] < Dimensions[workAxis1]; startPos[workAxis1]++)
                    {
                        for (startPos[workAxis2] = 0; startPos[workAxis2] < Dimensions[workAxis2]; startPos[workAxis2]++)
                        {
                            startBlock = blocks[(startPos.X * Width + startPos.Z) * Height + startPos.Y];

                            // If this block has already been merged, is air, or not visible skip it.
                            if (merged[startPos[workAxis1], startPos[workAxis2]] || startBlock == 0 || !IsBlockFaceVisible(startPos, direction, isBackFace))
                            {
                                continue;
                            }

                            // Reset the work var
                            quadSize = new Vector3I();

                            // Figure out the width, then save it
                            for (currPos = startPos, currPos[workAxis2]++; currPos[workAxis2] < Dimensions[workAxis2] && CompareStep(startPos, currPos, direction, isBackFace) && !merged[currPos[workAxis1], currPos[workAxis2]]; currPos[workAxis2]++) { }
                            quadSize[workAxis2] = currPos[workAxis2] - startPos[workAxis2];

                            // Figure out the height, then save it
                            for (currPos = startPos, currPos[workAxis1]++; currPos[workAxis1] < Dimensions[workAxis1] && CompareStep(startPos, currPos, direction, isBackFace) && !merged[currPos[workAxis1], currPos[workAxis2]]; currPos[workAxis1]++)
                            {
                                for (currPos[workAxis2] = startPos[workAxis2]; currPos[workAxis2] < Dimensions[workAxis2] && CompareStep(startPos, currPos, direction, isBackFace) && !merged[currPos[workAxis1], currPos[workAxis2]]; currPos[workAxis2]++) { }

                                // If we didn't reach the end then its not a good add.
                                if (currPos[workAxis2] - startPos[workAxis2] < quadSize[workAxis2])
                                {
                                    break;
                                }
                                else
                                {
                                    currPos[workAxis2] = startPos[workAxis2];
                                }
                            }
                            quadSize[workAxis1] = currPos[workAxis1] - startPos[workAxis1];

                            // Now we add the quad to the mesh
                            m = new Vector3I();
                            m[workAxis1] = quadSize[workAxis1];

                            n = new Vector3I();
                            n[workAxis2] = quadSize[workAxis2];

                            // We need to add a slight offset when working with front faces.
                            offsetPos = startPos;
                            offsetPos[direction] += isBackFace ? 0 : 1;

                            //Draw the face to the mesh
                            vertices = new Vector3I[] {
                                offsetPos,
                                offsetPos + m,
                                offsetPos + m + n,
                                offsetPos + n
                            };

                            byte cr = (byte)(startBlock >> 16 & 0xFF);
                            byte cg = (byte)(startBlock >> 8 & 0xFF);
                            byte cb = (byte)(startBlock & 0xFF);

                            List<VertexData> quad = quads[face];
                            if (!isBackFace)
                            {
                                AddPoint(quad, vertices[0], cr, cg, cb);
                                AddPoint(quad, vertices[1], cr, cg, cb);
                                AddPoint(quad, vertices[2], cr, cg, cb);


                                AddPoint(quad, vertices[0], cr, cg, cb);
                                AddPoint(quad, vertices[2], cr, cg, cb);
                                AddPoint(quad, vertices[3], cr, cg, cb);
                            }
                            else
                            {
                                AddPoint(quad, vertices[2], cr, cg, cb);
                                AddPoint(quad, vertices[1], cr, cg, cb);
                                AddPoint(quad, vertices[0], cr, cg, cb);


                                AddPoint(quad, vertices[3], cr, cg, cb);
                                AddPoint(quad, vertices[2], cr, cg, cb);
                                AddPoint(quad, vertices[0], cr, cg, cb);
                            }

                            // Mark it merged
                            for (int f = 0; f < quadSize[workAxis1]; f++)
                            {
                                for (int g = 0; g < quadSize[workAxis2]; g++)
                                {
                                    merged[startPos[workAxis1] + f, startPos[workAxis2] + g] = true;
                                }
                            }
                        }
                    }
                }
                void AddPoint(List<VertexData> quad, Vector3I p, byte r, byte g, byte b)
                {
                    quad.Add(new VertexData(p.X * scale, p.Y, p.Z * scale, face, r, g, b));

                    //quad.Add((ushort)((ushort)p.Z * scale | r << 9));
                    //quad.Add((ushort)(g | b << 8));
                    //quad.Add((ushort)(0 | nx << 4 | ny << 8 | nz << 12));
                }
            }
        }
#endif

        //private uint pack(int x, int y, int z, int normal, int r, int g, int b)
        //{

        //}

        [StructLayout(LayoutKind.Sequential)]
        struct NativeMeshData
        {
            VertexData[,] vertices;
            int[] lengths;
        }

        [DllImport("Native")]
        private static unsafe extern NativeMeshData Mesh(byte*** blocks, int Width, int Height, short scale, uint* colors, byte emission);

        public static VertexData[][] GenerateMeshFromBlocks(Model meshData, int Width, int Height, byte emission, int scale = 1)
        {

            //unsafe
            //{
            //    fixed (byte*** dataPtr = (byte***)meshData.blocks)
            //    {

            //    }
            //}

            List<VertexData>[] quads = new List<VertexData>[6] { new(), new(), new(), new(), new(), new() };

            GreedyMesh(quads, meshData.blocks, Width, Height, (short)scale, emission);

            VertexData[][] result = new VertexData[6][];
            for (int i = 0; i < 6; i++)
            {
                result[i] = quads[i].ToArray();
            }

            return result;
        }
    }
}
