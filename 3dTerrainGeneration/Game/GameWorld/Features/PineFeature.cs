using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Game.GameWorld.Features
{
    internal class PineFeature : IFeature
    {
        private List<Structure> trees;
        private TerrainGenerator terrainGenerator;

        public PineFeature(TerrainGenerator terrainGenerator)
        {
            this.terrainGenerator = terrainGenerator;

            trees = new List<Structure>();
            trees.Add(new ImportedStructure("trees/pine0.vox"));
            trees.Add(new ImportedStructure("trees/pine1.vox"));
            trees.Add(new ImportedStructure("trees/pine2.vox"));
            trees.Add(new ImportedStructure("trees/pine3.vox"));
        }

        public bool Inhabitable(BiomeInfo biome)
        {
            bool temperature = biome.Temperature > -30 && biome.Temperature < 15;
            bool humidity = biome.Humidity > 10 && biome.Humidity < 70;
            bool fertility = biome.Fertility > 10;

            return temperature && humidity && fertility;
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
            bool random = NoiseUtil.GetPerlin(X, Z + 6463, 2) > .98f;

            return isFertile && !road && random;
        }

        public void Process(Chunk chunk, ConcurrentDictionary<Vector3I, Chunk> chunks, HashSet<Chunk> modifiedChunks, int x, int y, int z, BiomeInfo biome, VoxelOctree octree)
        {
            int X = chunk.X * Chunk.CHUNK_SIZE + x;
            int Y = chunk.Y * Chunk.CHUNK_SIZE + y;
            int Z = chunk.Z * Chunk.CHUNK_SIZE + z;

            if (CanPlace(X, Y, Z, x, y, z, biome, octree))
            {
                Vector3I localPos = new Vector3I(x, y, z);
                int variation = (int)(terrainGenerator.Random(localPos) * (trees.Count - 1));

                terrainGenerator.PlaceStructure(chunk, chunks, modifiedChunks, trees[variation], localPos);
            }
        }
    }
}
