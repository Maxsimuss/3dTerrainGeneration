using _3dTerrainGeneration.world;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    public class AxisAlignedBB
    {
        public double width, height;

        public AxisAlignedBB(double width, double height)
        {
            this.width = width;
            this.height = height;
        }

        public bool isInBounds(double x, double y, double z) {
            return Math.Abs(x) < width && Math.Abs(z) < width && y < height && y >= 0;
        }

        public bool isColliding(double x, double y, double z, AxisAlignedBB box)
        {
            return isInBounds(x + box.width, y + box.height, z + box.width) ||
                isInBounds(x + box.width, y + box.height, z - box.width) ||
                isInBounds(x - box.width, y + box.height, z + box.width) ||
                isInBounds(x - box.width, y + box.height, z - box.width) ||
                isInBounds(x + box.width, y, z + box.width) ||
                isInBounds(x + box.width, y, z - box.width) ||
                isInBounds(x - box.width, y, z + box.width) ||
                isInBounds(x - box.width, y, z - box.width);
        }

        public bool isColliding(double x, double y, double z, World world)
        {
            return isColliding(new Vector3d(x, y, z), world);
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
