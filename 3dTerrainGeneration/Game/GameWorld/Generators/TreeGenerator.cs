using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using System;

namespace _3dTerrainGeneration.Game.GameWorld.Generators
{
    internal class TreeGenerator
    {
        private Random random;
        public TreeGenerator(int seed)
        {
            random = new Random(seed);
        }

        public Structure GenerateBlobTree()
        {
            Structure tree = new Structure();
            uint leaveColor = Color.HsvToRgb(random.NextDouble() * 170 + 290, random.NextDouble() * .25 + .5, 1);
            for (int y = 0; y < 20; y++)
            {
                tree.SetBlock(0, y, 0, Color.ToInt(120, 100, 80));
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
