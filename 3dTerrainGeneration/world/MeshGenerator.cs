using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.util;
using OpenTK;
using OpenTK.Graphics.ES20;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    class MeshGenerator
    {
        public static Materials materials = new Materials();

        public static void GreedyMesh(List<ushort> quads, byte[][][] blocks, int Size, short scale, List<uint> colors, byte emission, int yOffset = 0)
        {
            // Sweep over each axis (X, Y and Z)
            for (var d = 0; d < 3; ++d)
            {
                int i, j, k, l, w, h;
                int u = (d + 1) % 3;
                int v = (d + 2) % 3;
                var x = new int[3];
                var q = new int[3];

                byte[] mask = new byte[Size * Size];
                q[d] = 1;

                // Check each slice of the chunk one at a time
                for (x[d] = -1; x[d] < Size;)
                {
                    // Compute the mask
                    var n = 0;
                    for (x[v] = 0; x[v] < Size; ++x[v])
                    {
                        for (x[u] = 0; x[u] < Size; ++x[u])
                        {
                            // q determines the direction (X, Y or Z) that we are searching
                            // m.IsBlockAt(x,y,z) takes global map positions and returns true if a block exists there

                            byte blockCurrent = 0 <= x[d] ? blocks[x[0]][x[2]][x[1] + yOffset * Size] : (byte)0;
                            byte blockCompare = x[d] < Size - 1 ? blocks[x[0] + q[0]][x[2] + q[2]][x[1] + q[1] + yOffset * Size] : (byte)0;

                            // The mask is set to true if there is a visible face between two blocks,
                            //   i.e. both aren't empty and both aren't blocks
                            mask[n++] = blockCurrent != 0 && blockCompare != 0 && blockCurrent == blockCompare ? (byte)0 :
                                blockCurrent == 0 ? blockCompare : blockCurrent;
                        }
                    }

                    ++x[d];

                    n = 0;

                    // Generate a mesh from the mask using lexicographic ordering,      
                    //   by looping over each block in this slice of the chunk
                    for (j = 0; j < Size; ++j)
                    {
                        for (i = 0; i < Size;)
                        {
                            if (mask[n] != 0)
                            {
                                // Compute the width of this quad and store it in w                        
                                //   This is done by searching along the current axis until mask[n + w] is false
                                for (w = 1; i + w < Size && mask[n + w] != 0 && mask[n + w] == mask[n]; w++) { }

                                // Compute the height of this quad and store it in h                        
                                //   This is done by checking if every block next to this row (range 0 to w) is also part of the mask.
                                //   For example, if w is 5 we currently have a quad of dimensions 1 x 5. To reduce triangle count,
                                //   greedy meshing will attempt to expand this quad out to Chunk.Size x 5, but will stop if it reaches a hole in the mask

                                var done = false;
                                for (h = 1; j + h < Size; h++)
                                {
                                    // Check each block next to this quad
                                    for (k = 0; k < w; ++k)
                                    {
                                        // If there's a hole in the mask, exit
                                        if (mask[n + k + h * Size] == 0 || mask[n + k + h * Size] != mask[n])
                                        {
                                            done = true;
                                            break;
                                        }
                                    }

                                    if (done)
                                        break;
                                }

                                x[u] = i;
                                x[v] = j;

                                // du and dv determine the size and orientation of this face
                                var du = new int[3];
                                du[u] = w;

                                var dv = new int[3];
                                dv[v] = h;

                                Vector3 p0 = new Vector3(x[0], x[1] + yOffset * Size, x[2]);
                                Vector3 p1 = new Vector3(x[0] + du[0], x[1] + du[1] + yOffset * Size, x[2] + du[2]);
                                Vector3 p2 = new Vector3(x[0] + du[0] + dv[0], x[1] + du[1] + dv[1] + yOffset * Size, x[2] + du[2] + dv[2]);
                                Vector3 p3 = new Vector3(x[0] + dv[0], x[1] + dv[1] + yOffset * Size, x[2] + dv[2]);

                                Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).Normalized();

                                byte nx = (byte)Math.Round(normal.X);
                                byte ny = (byte)Math.Round(normal.Y);
                                byte nz = (byte)Math.Round(normal.Z);

                                uint color = colors[mask[n] - 1];
                                byte r = (byte)(color >> 16 & 0xFF);
                                byte g = (byte)(color >> 8 & 0xFF);
                                byte b = (byte)(color & 0xFF);

                                AddPoint(p0, r, g, b, nx, ny, nz);
                                AddPoint(p1, r, g, b, nx, ny, nz);
                                AddPoint(p2, r, g, b, nx, ny, nz);
                                
                                
                                AddPoint(p0, r, g, b, nx, ny, nz);
                                AddPoint(p2, r, g, b, nx, ny, nz);
                                AddPoint(p3, r, g, b, nx, ny, nz);

                                // Clear this part of the mask, so we don't add duplicate faces
                                for (l = 0; l < h; ++l)
                                    for (k = 0; k < w; ++k)
                                        mask[n + k + l * Size] = 0;

                                // Increment counters and continue
                                i += w;
                                n += w;
                            }
                            else
                            {
                                i++;
                                n++;
                            }
                        }
                    }
                }
            }

            void AddPoint(Vector3 p, byte r, byte g, byte b, byte nx, byte ny, byte nz)
            {
                quads.Add((ushort)((ushort)p.X * scale | (ushort)p.Y << 8));
                quads.Add((ushort)((ushort)p.Z * scale | r << 8));
                quads.Add((ushort)(g | b << 8));
                quads.Add((ushort)(emission | nx << 8 | ny << 9 | nz << 10));
            }
        }

        public static ushort[] GenerateMeshFromBlocks(MeshData meshData, int Width, int Height, byte emission, int scale = 1)
        {
            List<ushort> quads = new List<ushort>();

            for (int i = 0; i < Height / Width; i++)
            {
                GreedyMesh(quads, meshData.blocks, Width, (short)scale, meshData.pallette, emission, i);
            }
     
            return quads.ToArray();
        }
    }
}
