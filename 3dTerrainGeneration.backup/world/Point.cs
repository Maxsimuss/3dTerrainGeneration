using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    class Point : Structure
    {
        public Point(int xPos, int yPos, int zPos) : base(xPos, yPos, zPos)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        SetBlock(i - 1, j - 1, k - 1, MaterialType.SNOW);
                    }
                }
            }

            Mesh();
        }
    }
}
