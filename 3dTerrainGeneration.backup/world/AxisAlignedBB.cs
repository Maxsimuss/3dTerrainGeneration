using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    class AxisAlignedBB
    {
        public double width, height;

        public AxisAlignedBB(double width, double height)
        {
            this.width = width;
            this.height = height;
        }

        public bool isColliding(Vector3d position, World world)
        {
            for (int x = -1; x < 2; x++)
            {
                for (int z = -1; z < 2; z++)
                {
                    if(world.GetBlockAt(position.X + width * x, position.Y, position.Z + width * z) ||
                    world.GetBlockAt(position.X + width * x, position.Y + height, position.Z + width * z))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
