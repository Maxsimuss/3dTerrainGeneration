using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace _3dTerrainGeneration.Game.GameWorld.Features
{
    internal class CactusFeature : IFeature
    {
        private List<Structure> cactuses;
        private TerrainGenerator terrainGenerator;

        public CactusFeature(TerrainGenerator terrainGenerator)
        {
            this.terrainGenerator = terrainGenerator;

            cactuses = new List<Structure>();
            cactuses.Add(new ImportedStructure("Natural/Cactus/cactus0.vox"));
        }

        public bool Inhabitable(BiomeInfo biome)
        {
            bool temperature = biome.Temperature > 35;
            bool humidity = biome.Humidity < 50;
            bool fertility = biome.Fertility > 20;

            return temperature && humidity && fertility;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanPlace(int X, int Y, int Z, int x, int y, int z, BiomeInfo biome, uint[] octree)
        {
            if (y < 1 || octree[y] != 0)
            {
                return false;
            }

            bool isFertile = (octree[y - 1] & (uint)BlockMask.Fertile) != 0;
            bool road = (octree[y] & (uint)BlockMask.Road) != 0;
            bool random = NoiseUtil.GetPerlin(X, Z + 32148, 2) > .98f;

            return isFertile && !road && random;
        }

        public void Process(Chunk chunk, ChunkManager chunkManager, int x, int y, int z, BiomeInfo biome, uint[] octree)
        {
            int X = chunk.X * Chunk.CHUNK_SIZE + x;
            int Y = chunk.Y * Chunk.CHUNK_SIZE + y;
            int Z = chunk.Z * Chunk.CHUNK_SIZE + z;

            if (CanPlace(X, Y, Z, x, y, z, biome, octree))
            {
                Vector3I localPos = new Vector3I(x, y, z);
                int variation = (int)(terrainGenerator.Random(localPos) * (cactuses.Count - 1));

                terrainGenerator.PlaceStructure(chunk, chunkManager, cactuses[variation], localPos);
            }
        }
    }
}
