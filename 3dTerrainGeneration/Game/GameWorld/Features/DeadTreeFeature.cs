﻿using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace _3dTerrainGeneration.Game.GameWorld.Features
{
    internal class DeadTreeFeature : IFeature
    {
        private List<Structure> trees;
        private TerrainGenerator terrainGenerator;

        public DeadTreeFeature(TerrainGenerator terrainGenerator)
        {
            this.terrainGenerator = terrainGenerator;

            trees = new List<Structure>();
            trees.Add(new ImportedStructure("trees/deadtree0.vox"));
            trees.Add(new ImportedStructure("trees/deadtree1.vox"));
            trees.Add(new ImportedStructure("trees/deadtree2.vox"));
        }

        public bool Inhabitable(BiomeInfo biome)
        {
            bool extremeTemperature = biome.Temperature < -10;
            bool temperature = biome.Temperature < 20;
            bool humidity = biome.Humidity < 30;
            bool fertility = biome.Fertility < 40;

            return ((temperature && fertility) || extremeTemperature) && humidity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanPlace(int X, int Y, int Z, int x, int y, int z, BiomeInfo biome, VoxelOctree octree)
        {
            if (y < 1 || octree.GetValue(x, y, z) != 0)
            {
                return false;
            }

            bool isFertile = (octree.GetValue(x, y - 1, z) & (uint)BlockMask.Fertile) != 0;
            bool road = (octree.GetValue(x, y, z) & (uint)BlockMask.Road) != 0;
            bool random = NoiseUtil.GetPerlin(X, Z + 2084, 2) > .99f;

            return isFertile && !road && random;
        }

        public void Process(Chunk chunk, ChunkManager chunkManager, int x, int y, int z, BiomeInfo biome, VoxelOctree octree)
        {
            int X = chunk.X * Chunk.CHUNK_SIZE + x;
            int Y = chunk.Y * Chunk.CHUNK_SIZE + y;
            int Z = chunk.Z * Chunk.CHUNK_SIZE + z;

            if (CanPlace(X, Y, Z, x, y, z, biome, octree))
            {
                Vector3I localPos = new Vector3I(x, y, z);
                int variation = (int)(terrainGenerator.Random(localPos) * (trees.Count - 1));

                terrainGenerator.PlaceStructure(chunk, chunkManager, trees[variation], localPos);
            }
        }
    }
}
