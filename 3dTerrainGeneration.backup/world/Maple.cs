using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    class Maple : Structure
    {
        public Maple(int xPos, int yPos, int zPos) : base(xPos, yPos, zPos)
        {
            Random rnd = new Random(xPos + yPos + zPos);

            byte leaves = rnd.NextDouble() > .5 ? MaterialType.MAPLE_LEAVES1 : MaterialType.MAPLE_LEAVES2;
            int rndOffset = rnd.Next();

            for (int i = 1; i <= rnd.Next(2, 4); i++)
            {
                int wid = rnd.Next(3, 6);
                for (int x = -wid; x <= wid; x++)
                {
                    for (int z = -wid; z <= wid; z++)
                    {
                        for (int y = -wid; y < wid; y++)
                        {
                            if (Math.Sqrt(x * x + z * z + y * y * 2) <= wid)
                                SetBlock(x + (int)(Math.Sin(i * 4 + rndOffset) * wid / 2), y + wid + i * 6, z + (int)(Math.Sin(i * 4 + 10 + rndOffset) * wid / 2), leaves);
                        }
                    }
                }
            }

            for (int h = 0; h < yMax - 2; h++)
            {
                SetBlock(0, h, 0, MaterialType.WOOD);
            }

            Mesh();
        }
    }
}
