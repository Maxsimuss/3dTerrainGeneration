using OpenTK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    class Structure
    {
        Dictionary<Vector3, byte> data = new Dictionary<Vector3, byte>();
        byte[][][] blockData;

        public int xPos, yPos, zPos;

        protected int xMin, yMin, zMin;
        protected int xMax, yMax, zMax;

        public int blocks = 0;
        protected Structure(int xPos, int yPos, int zPos)
        {
            this.xPos = xPos;
            this.yPos = yPos;
            this.zPos = zPos;
        }

        protected void SetBlock(int x, int y, int z, byte type)
        {
            xMax = Math.Max(xMax, x);
            yMax = Math.Max(yMax, y);
            zMax = Math.Max(zMax, z);

            xMin = Math.Min(xMin, x);
            yMin = Math.Min(yMin, y);
            zMin = Math.Min(zMin, z);

            data[new Vector3(x, y, z)] = type;
            blocks++;
        }

        public void Mesh()
        {
            int xL = xMax - xMin + 1;
            int yL = yMax - yMin + 1;
            int zL = zMax - zMin + 1;

            xPos += xMin;
            yPos += yMin;
            zPos += zMin;

            blockData = new byte[xL][][];
            for (int i = 0; i < xL; i++)
            {
                blockData[i] = new byte[yL][];
                for (int j = 0; j < yL; j++)
                {
                    blockData[i][j] = new byte[zL];
                    for (int k = 0; k < zL; k++)
                    {
                        Vector3 loc = new Vector3(i + xMin, j + yMin, k + zMin);

                        if(data.ContainsKey(loc))
                            blockData[i][j][k] = data[loc];
                    }
                }
            }

            data = null;
        }

        public bool Spawn(ref byte[][][] inData, int x, int z)
        {
            bool modified = false;
            int xL = xMax - xMin;
            int yL = yMax - yMin;
            int zL = zMax - zMin;

            for (int i = 0; i <= xL; i++)
            {
                int X = i + xPos - x;
                if (X >= Chunk.Width || X < 0) continue;
                for (int j = 0; j <= yL; j++)
                {
                    int Y = j + yPos;
                    if (Y >= Chunk.Height || Y < 0) continue;
                    for (int k = 0; k <= zL; k++)
                    {
                        int Z = k + zPos - z;
                        if (Z >= Chunk.Width || Z < 0) continue;

                        if(blockData[i][j][k] != 0)
                        {
                            inData[X][Z][Y] = blockData[i][j][k];
                            blocks--;
                            modified = true;
                        }
                    }
                }
            }

            return modified;
        }
    }
}
