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
        public FastNoiseLite noise = new FastNoiseLite();
        BiomeMap map;
        public Materials materials;

        public MeshGenerator()
        {
            noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            noise.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2Reduced);
            noise.SetDomainWarpAmp(100);
            noise.SetFrequency(.005f);
            map = new BiomeMap(noise);
            materials = new Materials();
        }

        private double GetPerlin(int x, int z, double scale)
        {
            //scale /= 10d;
            return noise.GetNoise((float)(x * scale), (float)(z * scale)) + .5;
        }


        public double GetHeight(int x, int z)
        {
            double mountains = GetPerlin(x, z, 1) * 40;
            mountains += GetPerlin(x, z, 2) * 20;
            mountains += GetPerlin(x, z, 4) * 10;
            mountains += GetPerlin(x, z, 8) * 5;
            mountains += GetPerlin(x, z, 16) * 2.5;
            mountains += GetPerlin(x, z, 64) * 1;

            double sharpMountains = Math.Pow(mountains, 3) / 2000;
            mountains *= 2;

            double smallHills = GetPerlin(x, z, 8) * 10;
            double plains = GetPerlin(x, z, 1) * 10;

            BiomeData biomeData = map.GetBiomeData(x, z);

            double mountainVal = Lerp(sharpMountains, mountains, biomeData.mountainSharpness);
            double plainHills = Lerp(plains, smallHills, biomeData.mountainSharpness);

            return Lerp(mountainVal, plainHills, biomeData.mountainess) + biomeData.baseHeight;
        }

        private double Lerp(double d1, double d2, double val)
        {
            val = Math.Max(0, Math.Min(val, 1));

            return d1 * val + d2 * (1d - val);
        }

        public void GreedyMesh(List<short> quads, byte[][][] blocks, int Width, int yOffset, short scale, byte block)
        {
            Vector3 color = materials.materials[block].color;
            short r = (short)color.X;
            short g = (short)color.Y;
            short b = (short)color.Z;

            // Sweep over each axis (X, Y and Z)
            for (var d = 0; d < 3; ++d)
            {
                int i, j, k, l, w, h;
                int u = (d + 1) % 3;
                int v = (d + 2) % 3;
                var x = new int[3];
                var q = new int[3];

                var mask = new bool[Width * Width];
                q[d] = 1;

                // Check each slice of the chunk one at a time
                for (x[d] = -1; x[d] < Width;)
                {
                    // Compute the mask
                    var n = 0;
                    for (x[v] = 0; x[v] < Width; ++x[v])
                    {
                        for (x[u] = 0; x[u] < Width; ++x[u])
                        {
                            // q determines the direction (X, Y or Z) that we are searching
                            // m.IsBlockAt(x,y,z) takes global map positions and returns true if a block exists there

                            byte blockCurrent = 0 <= x[d] ? blocks[x[0]][x[2]][x[1] + yOffset * Width] : (byte)0;
                            byte blockCompare = x[d] < Width - 1 ? blocks[x[0] + q[0]][x[2] + q[2]][x[1] + q[1] + yOffset * Width] : (byte)0;

                            // The mask is set to true if there is a visible face between two blocks,
                            //   i.e. both aren't empty and both aren't blocks
                            mask[n++] = ((blockCurrent == 0) != (blockCompare == 0)) && (blockCurrent == block || blockCompare == block);
                        }
                    }

                    ++x[d];

                    n = 0;

                    // Generate a mesh from the mask using lexicographic ordering,      
                    //   by looping over each block in this slice of the chunk
                    for (j = 0; j < Width; ++j)
                    {
                        for (i = 0; i < Width;)
                        {
                            if (mask[n])
                            {
                                // Compute the width of this quad and store it in w                        
                                //   This is done by searching along the current axis until mask[n + w] is false
                                for (w = 1; i + w < Width && mask[n + w]; w++) { }

                                // Compute the height of this quad and store it in h                        
                                //   This is done by checking if every block next to this row (range 0 to w) is also part of the mask.
                                //   For example, if w is 5 we currently have a quad of dimensions 1 x 5. To reduce triangle count,
                                //   greedy meshing will attempt to expand this quad out to Chunk.Size x 5, but will stop if it reaches a hole in the mask

                                var done = false;
                                for (h = 1; j + h < Width; h++)
                                {
                                    // Check each block next to this quad
                                    for (k = 0; k < w; ++k)
                                    {
                                        // If there's a hole in the mask, exit
                                        if (!mask[n + k + h * Width])
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

                                Vector3 v1 = new Vector3(x[0], x[1], x[2]);
                                Vector3 v2 = new Vector3(x[0] + du[0], x[1] + du[1], x[2] + du[2]);
                                Vector3 v3 = new Vector3(x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2]);

                                Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1);
                                Vector3 norm = new Vector3(du[0] - dv[0], du[1] - dv[1], du[2] - dv[2]);
                                //normal = normal *  norm;
                                //Console.WriteLine(normal);
                                normal.Normalize();
                                //normal += new Vector3(1);

                                short nX = (short)Math.Round(normal.X);
                                short nY = (short)Math.Round(normal.Y);
                                short nZ = (short)Math.Round(normal.Z);

                                //if(norm.Y < 0)
                                //{
                                //    nY -= 2;
                                //}

                                quads.Add((short)(x[0] * scale));
                                quads.Add((short)(x[1] + yOffset * Width));
                                quads.Add((short)(x[2] * scale));
                                quads.Add(nX);
                                quads.Add(nY);
                                quads.Add(nZ);
                                quads.Add(r);
                                quads.Add(g);
                                quads.Add(b);
                                quads.Add((short)((x[0] + du[0]) * scale));
                                quads.Add((short)(x[1] + du[1] + yOffset * Width));
                                quads.Add((short)((x[2] + du[2]) * scale));
                                quads.Add(nX);
                                quads.Add(nY);
                                quads.Add(nZ);
                                quads.Add(r);
                                quads.Add(g);
                                quads.Add(b);
                                quads.Add((short)((x[0] + du[0] + dv[0]) * scale));
                                quads.Add((short)(x[1] + du[1] + dv[1] + yOffset * Width));
                                quads.Add((short)((x[2] + du[2] + dv[2]) * scale));
                                quads.Add(nX);
                                quads.Add(nY);
                                quads.Add(nZ);
                                quads.Add(r);
                                quads.Add(g);
                                quads.Add(b);
                                quads.Add((short)((x[0] + dv[0]) * scale));
                                quads.Add((short)(x[1] + dv[1] + yOffset * Width));
                                quads.Add((short)((x[2] + dv[2]) * scale));
                                quads.Add(nX);
                                quads.Add(nY);
                                quads.Add(nZ);
                                quads.Add(r);
                                quads.Add(g);
                                quads.Add(b);

                                // Clear this part of the mask, so we don't add duplicate faces
                                for (l = 0; l < h; ++l)
                                    for (k = 0; k < w; ++k)
                                        mask[n + k + l * Width] = false;

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
        }

        public short[] GenerateMeshFromBlocks(byte[][][] blocks, int Size, int Height, short lod, bool[][] usedBlocks)
        {
            List<short> quads = new List<short>();

            for (int i = 0; i < Height / Size * lod; i++)
            {
                for (byte block = 1; block <= materials.materials.Count; block++)
                {
                    if (usedBlocks[i / lod][block - 1])
                        GreedyMesh(quads, blocks, Size / lod, i, lod, block);
                }
            }

            return quads.ToArray();
        }
    }
}
