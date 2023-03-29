﻿using _3dTerrainGeneration.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    internal class TreeGenerator
    {
        Random random;
        public TreeGenerator(int seed)
        {
            random = new Random(1234);
        }

        public Structure GenerateBlobTree()
        {
            Structure tree = new Structure();
            uint leaveColor = Color.HsvToRgb(random.NextDouble() * 170 + 290, random.NextDouble() * .25 + .5, 1);
            for (int y = 0; y < 20; y++)
            {
                tree.SetBlock(0, y, 0, 0x664422);
            }

            int max = 20;
            int min = 5;

            int leaveCount = random.Next(3, 6);
            for (int i = 0; i < leaveCount; i++)
            {
                int size = 3 + i / 2;
                PlaceLeaves(tree, random.Next(-size + 1, size), (int)((float)i / (leaveCount - 1) * (max - min) + min), random.Next(-size + 1, size), size * 2, leaveColor);
            }
            return tree;
        }

        private void PlaceLeaves(Structure tree, int X, int Y, int Z, int size, uint leaveColor)
        {
            for (int x = -size; x < size; x++)
            {
                for (int y = -size; y < size; y++)
                {
                    for (int z = -size; z < size; z++)
                    {
                        if (MathF.Sqrt(x * x + y * y + z * z) < size / 2)
                        {
                            tree.SetBlock(X + x, Y + y, Z + z, leaveColor);
                        }
                    }
                }
            }
        }
    }
}
