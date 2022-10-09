using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.util;
using OpenTK;
using OpenTK.Graphics.ES20;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    struct Vector3I
    {
        public int X, Y, Z;

        public Vector3I(int X, int Y, int Z)
        {
            this.X = X; this.Y = Y; this.Z = Z;
        }

        public static Vector3I operator+ (Vector3I left, Vector3I right)
        {
            return new Vector3I(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public int this[int index] {
            get
            {
                return index switch
                {
                    0 => X,
                    1 => Y,
                    2 => Z,
                    _ => throw new IndexOutOfRangeException("You tried to access this vector at index: " + index),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    case 2:
                        Z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
                }
            }
        }
    }

    class MeshGenerator
    {
        public static Materials materials = new Materials();

        public static void GreedyMesh(List<uint>[] quads, byte[][][] blocks, int Width, int Height, short scale, List<uint> colors, byte emission)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            byte GetBlock(Vector3I blockPosition)
            {
                if(blockPosition.X >= Width || blockPosition.Z >= Width || blockPosition.Y >= Height ||
                    blockPosition.X < 0 || blockPosition.Z < 0 || blockPosition.Y < 0)
                {
                    return 0;
                }

                return blocks[(int)blockPosition.X][(int)blockPosition.Z][(int)blockPosition.Y];
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
                byte blockA = GetBlock(a);
                byte blockB = GetBlock(b);

                return blockA == blockB && blockB != 0 && IsBlockFaceVisible(b, direction, backFace);
            }

            Vector3I Dimensions = new Vector3I(Width, Height, Width);
            bool[,] merged;

            Vector3I startPos, currPos, quadSize, m, n, offsetPos;
            Vector3I[] vertices;

            byte startBlock;
            int direction, workAxis1, workAxis2;

            // Iterate over each face of the blocks.
            for (int face = 0; face < 6; face++)
            {
                bool isBackFace = face > 2;
                direction = face % 3;
                byte nx = (byte)(direction == 0 ? (isBackFace ? 0 : 14) : 7);
                byte ny = (byte)(direction == 1 ? (isBackFace ? 0 : 14) : 7);
                byte nz = (byte)(direction == 2 ? (isBackFace ? 0 : 14) : 7);

                workAxis1 = (direction + 1) % 3;
                workAxis2 = (direction + 2) % 3;

                startPos = new Vector3I();
                currPos = new Vector3I();

                // Iterate over the chunk layer by layer.
                for (startPos[direction] = 0; startPos[direction] < Dimensions[direction]; startPos[direction]++)
                {
                    merged = new bool[(int)Dimensions[workAxis1], (int)Dimensions[workAxis2]];

                    // Build the slices of the mesh.
                    for (startPos[workAxis1] = 0; startPos[workAxis1] < Dimensions[workAxis1]; startPos[workAxis1]++)
                    {
                        for (startPos[workAxis2] = 0; startPos[workAxis2] < Dimensions[workAxis2]; startPos[workAxis2]++)
                        {
                            startBlock = blocks[(int)startPos.X][(int)startPos.Z][(int)startPos.Y];

                            // If this block has already been merged, is air, or not visible skip it.
                            if (merged[(int)startPos[workAxis1], (int)startPos[workAxis2]] || startBlock == 0 || !IsBlockFaceVisible(startPos, direction, isBackFace))
                            {
                                continue;
                            }

                            // Reset the work var
                            quadSize = new Vector3I();

                            // Figure out the width, then save it
                            for (currPos = startPos, currPos[workAxis2]++; currPos[workAxis2] < Dimensions[workAxis2] && CompareStep(startPos, currPos, direction, isBackFace) && !merged[(int)currPos[workAxis1], (int)currPos[workAxis2]]; currPos[workAxis2]++) { }
                            quadSize[workAxis2] = currPos[workAxis2] - startPos[workAxis2];

                            // Figure out the height, then save it
                            for (currPos = startPos, currPos[workAxis1]++; currPos[workAxis1] < Dimensions[workAxis1] && CompareStep(startPos, currPos, direction, isBackFace) && !merged[(int)currPos[workAxis1], (int)currPos[workAxis2]]; currPos[workAxis1]++)
                            {
                                for (currPos[workAxis2] = startPos[workAxis2]; currPos[workAxis2] < Dimensions[workAxis2] && CompareStep(startPos, currPos, direction, isBackFace) && !merged[(int)currPos[workAxis1], (int)currPos[workAxis2]]; currPos[workAxis2]++) { }

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

                            uint color = colors[startBlock - 1];
                            byte cr = (byte)(color >> 16 & 0xFF);
                            byte cg = (byte)(color >> 8 & 0xFF);
                            byte cb = (byte)(color & 0xFF);

                            List<uint> quad = quads[face];
                            if(!isBackFace)
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
                                    merged[(int)(startPos[workAxis1] + f), (int)(startPos[workAxis2] + g)] = true;
                                }
                            }
                        }
                    }
                }
                void AddPoint(List<uint> quad, Vector3I p, byte r, byte g, byte b)
                {
                    uint val = (uint)((byte)(p.X * scale) << 25 | (byte)(p.Y) << 18 | (byte)(p.Z * scale) << 11 | ((byte)face) << 8 | 
                        (((byte)(r / 36)) & (byte)7) << 5 | (((byte)(g / 36)) & (byte)7) << 2 | (((byte)(b / 85)) & 0x03));

                    quad.Add(val);
                    //quad.Add((ushort)((ushort)p.Z * scale | r << 9));
                    //quad.Add((ushort)(g | b << 8));
                    //quad.Add((ushort)(0 | nx << 4 | ny << 8 | nz << 12));
                }
            }
        }

        public static uint[][] GenerateMeshFromBlocks(MeshData meshData, int Width, int Height, byte emission, int scale = 1)
        {
            List<uint>[] quads = new List<uint>[6] { new(), new(), new(), new(), new(), new() };

            GreedyMesh(quads, meshData.blocks, Width, Height, (short)scale, meshData.palette, emission);

            uint[][] result = new uint[6][];
            for (int i = 0; i < 6; i++)
            {
                result[i] = quads[i].ToArray();
            }
            return result;
        }
    }
}
