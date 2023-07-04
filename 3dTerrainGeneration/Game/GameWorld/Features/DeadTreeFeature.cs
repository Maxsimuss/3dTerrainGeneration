using _3dTerrainGeneration.Engine.Util;
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
            trees.Add(new ImportedStructure("Natural/DeadTree/deadtree0.vox"));
            trees.Add(new ImportedStructure("Natural/DeadTree/deadtree1.vox"));
            trees.Add(new ImportedStructure("Natural/DeadTree/deadtree2.vox"));
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
        private bool CanPlace(int X, int Y, int Z, int x, int y, int z, BiomeInfo biome, uint[] octree)
        {
            if (y < 1 || octree[y] != 0)
            {
                return false;
            }

            bool isFertile = (octree[y - 1] & (uint)BlockMask.Fertile) != 0;
            bool road = (octree[y] & (uint)BlockMask.Road) != 0;
            bool random = NoiseUtil.GetPerlin(X, Z + 2084, 2) > .99f;

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
                int variation = (int)(terrainGenerator.Random(localPos) * (trees.Count - 1));

                terrainGenerator.PlaceStructure(chunk, chunkManager, trees[variation], localPos);
            }
        }
    }
}
