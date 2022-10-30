using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace _3dTerrainGeneration.world
{
    public class Structure
    {
        Dictionary<Vector3, byte> data = new Dictionary<Vector3, byte>();

        public int xPos, yPos, zPos;

        public int xMin = int.MaxValue, yMin = int.MaxValue, zMin = int.MaxValue;
        public int xMax = int.MinValue, yMax = int.MinValue, zMax = int.MinValue;

        public int blocks = 0;

        protected Structure(int xPos, int yPos, int zPos)
        {
            this.xPos = xPos;
            this.yPos = yPos;
            this.zPos = zPos;
        }

        protected void SetBlock(int x, int y, int z, uint color)
        {
            xMax = Math.Max(xMax, x);
            yMax = Math.Max(yMax, y);
            zMax = Math.Max(zMax, z);

            xMin = Math.Min(xMin, x);
            yMin = Math.Min(yMin, y);
            zMin = Math.Min(zMin, z);

            data[new Vector3(x, y, z)] = (byte)(Materials.IdOf(color));
            blocks++;
        }

        public void Mesh()
        {
            int xL = xMax - xMin;
            int yL = yMax - yMin;
            int zL = zMax - zMin;

            Dictionary<Vector3, byte> modified = new Dictionary<Vector3, byte>();
            foreach (var item in data)
            {
                int x = (int)item.Key.X - xL / 2 - xMin;
                int y = (int)item.Key.Y - yMin;
                int z = (int)item.Key.Z - zL / 2 - zMin;

                modified.Add(new Vector3(x, y, z), item.Value);
            }

            data = modified;
        }

        public bool Spawn(ref byte[] inData, ref object dataLock, int x, int y, int z)
        {
            if (blocks == 0) return false;

            bool modified = false;

            foreach (var item in data)
            {
                int X = (int)item.Key.X - x + xPos;
                int Y = (int)item.Key.Y - y + yPos;
                int Z = (int)item.Key.Z - z + zPos;

                if (X < 0 || Y < 0 || Z < 0 || X >= Chunk.Size || Y >= Chunk.Size || Z >= Chunk.Size) continue;

                if (inData == null)
                {
                    inData = new byte[Chunk.Size * Chunk.Size * Chunk.Size];
                }

                //Chunk.SetValue(inData, X, Z, Y, item.Value);
                blocks--;
                modified = true;
            }

            return modified;
        }
    }
}
