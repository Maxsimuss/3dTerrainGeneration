using _3dTerrainGeneration.Engine.Util;
using System;
using System.Collections.Generic;

namespace _3dTerrainGeneration.Game.GameWorld.Structures
{
    public class Structure
    {
        public Dictionary<Vector3I, uint> Data = new Dictionary<Vector3I, uint>();

        public int xMin = int.MaxValue, yMin = int.MaxValue, zMin = int.MaxValue;
        public int xMax = int.MinValue, yMax = int.MinValue, zMax = int.MinValue;

        public void SetBlock(int x, int y, int z, uint color)
        {
            xMax = Math.Max(xMax, x);
            yMax = Math.Max(yMax, y);
            zMax = Math.Max(zMax, z);

            xMin = Math.Min(xMin, x);
            yMin = Math.Min(yMin, y);
            zMin = Math.Min(zMin, z);

            Data[new Vector3I(x, y, z)] = color;
        }

        public void Mesh()
        {
            int xL = xMax - xMin;
            int yL = yMax - yMin;
            int zL = zMax - zMin;

            Dictionary<Vector3I, uint> modified = new Dictionary<Vector3I, uint>();
            foreach (var item in Data)
            {
                int x = item.Key.X - xL / 2 - xMin;
                int y = item.Key.Y - yMin;
                int z = item.Key.Z - zL / 2 - zMin;

                modified.Add(new(x, y, z), item.Value);
            }

            Data = modified;
        }
    }
}
