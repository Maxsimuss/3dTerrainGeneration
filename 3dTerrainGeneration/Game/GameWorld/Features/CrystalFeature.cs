using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace _3dTerrainGeneration.Game.GameWorld.Features
{
    internal class CrystalFeature : IFeature
    {
        private List<Structure> crystals;
        private TerrainGenerator terrainGenerator;

        public CrystalFeature(TerrainGenerator terrainGenerator)
        {
            this.terrainGenerator = terrainGenerator;

            crystals = new List<Structure>();

            for (int i = 0; i < 16; i++)
            {
                crystals.Add(new ImportedStructure(string.Format("crystals/crystal{0}{1}.vox", i / 4, i % 4)));
            }
        }

        public bool Inhabitable(BiomeInfo biome)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanPlace(int X, int Y, int Z, int x, int y, int z, BiomeInfo biome, uint[] octree)
        {
            if (y >= Chunk.CHUNK_SIZE - 1 || octree[y] == 0 || octree[y + 1] != 0)
            {
                return false;
            }

            bool random = NoiseUtil.GetPerlin(X, Z + 4321, 2) > 1 - (-Math.Clamp(Y, -200, 0) / 200.0) * .01;
            bool structure = (octree[y] & (uint)BlockMask.Structure) != 0;
            bool road = (octree[y] & (uint)BlockMask.Road) != 0;

            return random && !structure && !road;
        }

        public void Process(Chunk chunk, ChunkManager chunkManager, int x, int y, int z, BiomeInfo biome, uint[] octree)
        {
            int X = chunk.X * Chunk.CHUNK_SIZE + x;
            int Y = chunk.Y * Chunk.CHUNK_SIZE + y;
            int Z = chunk.Z * Chunk.CHUNK_SIZE + z;

            if (CanPlace(X, Y, Z, x, y, z, biome, octree))
            {
                Vector3I localPos = new Vector3I(x, y - 1, z);
                int variation = (int)(terrainGenerator.Random(localPos) * (crystals.Count - 1));

                terrainGenerator.PlaceStructure(chunk, chunkManager, crystals[variation], localPos);
            }
        }
    }
}
