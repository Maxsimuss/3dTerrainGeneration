using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    class Pine : Structure
    {
        public Pine(int xPos, int yPos, int zPos) : base(xPos, yPos, zPos)
        {
            Random rnd = new Random(xPos + yPos + zPos);

            byte leaves = rnd.NextDouble() > .5 ? MaterialType.PINE_LEAVES1 : MaterialType.PINE_LEAVES2;

            for (int h = 0; h < 20; h++)
            {
                if (h < 4)
                {
                    SetBlock(0, h, 0, MaterialType.WOOD);
                    SetBlock(-1, h, 0, MaterialType.WOOD);
                    SetBlock(1, h, 0, MaterialType.WOOD);
                    SetBlock(0, h, -1, MaterialType.WOOD);
                    SetBlock(0, h, 1, MaterialType.WOOD);
                    continue;
                }

                int wid = (-h + 20) / 3;

                for (int x = -wid; x <= wid; x++)
                {
                    for (int z = -wid; z <= wid; z++)
                    {
                        if (Math.Sqrt(x * x + z * z) <= wid)
                            SetBlock(x, h, z, leaves);
                    }
                }
            }

            Mesh();
        }
    }
}
