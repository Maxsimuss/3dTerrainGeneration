using System;
using System.Collections.Generic;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Models
{
    class Model
    {
        Dictionary<Vector3, uint> data = new Dictionary<Vector3, uint>();
        public uint[] blocks;

        protected int xMax, yMax, zMax;
        protected int xMin = int.MaxValue, yMin = int.MaxValue, zMin = int.MaxValue;

        public int Width, Height;

        public VertexData[] MeshSingle(byte emission, int scale = 1)
        {
            VertexData[][] mesh = Mesh(emission, scale);

            int len = 0;
            for (int i = 0; i < 6; i++)
            {
                len += mesh[i].Length;
            }

            VertexData[] meshSingle = new VertexData[len];
            int index = 0;
            for (int i = 0; i < 6; i++)
            {
                Array.Copy(mesh[i], 0, meshSingle, index, mesh[i].Length);
                index += mesh[i].Length;
            }

            return meshSingle;
        }

        public VertexData[][] Mesh(byte emission, int scale = 1)
        {
            if (blocks == null)
            {
                int xL = xMax - xMin + 1;
                int yL = yMax - yMin + 1;
                int zL = zMax - zMin + 1;
                Width = Math.Max(xL, zL);
                Height = (int)Math.Ceiling(yL / (double)Width) * Width;

                int xOff = 0;
                int zOff = 0;

                if (xL != zL)
                {
                    if (xL > zL)
                    {
                        zOff = (xL - zL) / 2;
                    }
                    else
                    {
                        xOff = (zL - xL) / 2;
                    }
                }

                blocks = new uint[Width * Width * Height];

                foreach (var item in data)
                {
                    blocks[((int)(item.Key.X - xMin + xOff) * Width + (int)(item.Key.Y - zMin + zOff)) * Height + (int)(item.Key.Z - yMin)] = item.Value;
                }

                data = null;
                Height = yL;
                return MeshGenerator.GenerateMeshFromBlocks(this, Width, (int)Math.Ceiling(yL / (double)Width) * Width, emission, scale);
            }

            return MeshGenerator.GenerateMeshFromBlocks(this, Width, (int)Math.Ceiling(Height / (double)Width) * Width, emission, scale);
        }

        public void SetDimensions(int w, int h)
        {
            Width = w;
            Height = h;

            blocks = new uint[Width * Width * Height];
        }

        public void SetBlockUnsafe(int x, int y, int z, uint type)
        {
            blocks[(x * Width + z) * Height + y] = type;
        }

        public void SetBlock(int x, int y, int z, uint type)
        {
            xMax = Math.Max(xMax, x);
            yMax = Math.Max(yMax, y);
            zMax = Math.Max(zMax, z);

            xMin = Math.Min(xMin, x);
            yMin = Math.Min(yMin, y);
            zMin = Math.Min(zMin, z);

            data[new Vector3(x, z, y)] = type;
        }
    }
}
