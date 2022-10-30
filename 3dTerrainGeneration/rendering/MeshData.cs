using _3dTerrainGeneration.world;
using OpenTK;
using System;
using System.Numerics;
using System.Collections.Generic;

namespace _3dTerrainGeneration.rendering
{
    class MeshData
    {
        Dictionary<Vector3, byte> data = new Dictionary<Vector3, byte>();
        public List<uint> palette = new List<uint>();
        public byte[][][] blocks;

        protected int xMax, yMax, zMax;
        protected int xMin = int.MaxValue, yMin = int.MaxValue, zMin = int.MaxValue;

        public int Width, Height;

        public uint[] MeshSingle(byte emission, int scale = 1)
        {
            uint[][] mesh = Mesh(emission, scale);

            int len = 0;
            for (int i = 0; i < 6; i++)
            {
                len += mesh[i].Length;
            }

            uint[] meshSingle = new uint[len];
            int index = 0;
            for (int i = 0; i < 6; i++)
            {
                Array.Copy(mesh[i], 0, meshSingle, index, mesh[i].Length);
                index += mesh[i].Length;
            }

            return meshSingle;
        }

        public uint[][] Mesh(byte emission, int scale = 1)
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

                blocks = new byte[Width][][];
                for (int i = 0; i < Width; i++)
                {
                    blocks[i] = new byte[Width][];
                    for (int j = 0; j < Width; j++)
                    {
                        blocks[i][j] = new byte[Height];
                    }
                }

                foreach (var item in data)
                {
                    blocks[(int)(item.Key.X - xMin + xOff)][(int)(item.Key.Y - zMin + zOff)][(int)(item.Key.Z - yMin)] = item.Value;
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

            blocks = new byte[Width][][];
            for (int i = 0; i < Width; i++)
            {
                blocks[i] = new byte[Width][];
                for (int j = 0; j < Width; j++)
                {
                    blocks[i][j] = new byte[Height];
                }
            }
        }

        public void SetBlockUnsafe(int x, int y, int z, byte type)
        {
            blocks[x][z][y] = (byte)(type + 1);
        }

        public void SetBlockUnsafe(int x, int y, int z, uint type)
        {
            if (!palette.Contains(type))
            {
                palette.Add(type);
            }
            int ind = palette.IndexOf(type);
            blocks[x][z][y] = (byte)(ind + 1);
        }

        public void SetBlock(int x, int y, int z, uint type)
        {
            xMax = Math.Max(xMax, x);
            yMax = Math.Max(yMax, y);
            zMax = Math.Max(zMax, z);

            xMin = Math.Min(xMin, x);
            yMin = Math.Min(yMin, y);
            zMin = Math.Min(zMin, z);

            if (!palette.Contains(type))
            {
                palette.Add(type);
            }
            int ind = palette.IndexOf(type);
            data[new Vector3(x, z, y)] = (byte)(ind + 1);
        }
    }
}
