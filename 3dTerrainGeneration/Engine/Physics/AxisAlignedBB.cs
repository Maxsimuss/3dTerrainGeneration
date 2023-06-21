using _3dTerrainGeneration.Engine.World;
using System;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Physics
{
    internal class AxisAlignedBB
    {
        public float width, height;

        public AxisAlignedBB(float width, float height)
        {
            this.width = width;
            this.height = height;
        }

        public bool isInBounds(float x, float y, float z)
        {
            return Math.Abs(x) < width && Math.Abs(z) < width && y < height && y >= 0;
        }

        public bool isColliding(float x, float y, float z, AxisAlignedBB box)
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

        public bool isColliding(float x, float y, float z, IWorld world)
        {
            return isColliding(new Vector3(x, y, z), world);
        }

        public bool isColliding(Vector3 position, IWorld world)
        {
            for (int x = -1; x < 2; x++)
            {
                for (int z = -1; z < 2; z++)
                {
                    if (world.GetBlockAt(position.X + width * x, position.Y, position.Z + width * z) != 0 ||
                    world.GetBlockAt(position.X + width * x, position.Y + height, position.Z + width * z) != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
