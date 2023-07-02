using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace _3dTerrainGeneration.Game.GameWorld.Features
{
    internal class RockFeature : IFeature
    {
        private List<Structure> rocks;
        private TerrainGenerator terrainGenerator;

        public RockFeature(TerrainGenerator terrainGenerator)
        {
            this.terrainGenerator = terrainGenerator;

            rocks = new List<Structure>();

            for (int i = 0; i < 4; i++)
            {
                rocks.Add(new ImportedStructure(string.Format("trees/rock0/rock{0}.vox", i)));
            }
        }

        public bool Inhabitable(BiomeInfo biome)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanPlace(int X, int Y, int Z, int x, int y, int z, BiomeInfo biome, VoxelOctree octree)
        {
            if (y > Chunk.CHUNK_SIZE - 1 || octree.GetValue(x, y, z) == 0 || octree.GetValue(x, y + 1, z) != 0)
            {
                return false;
            }

            bool random = NoiseUtil.GetPerlin(X, Z + 31241, 2) > .99f;
            bool structure = (octree.GetValue(x, y, z) & (uint)BlockMask.Structure) != 0;
            bool road = (octree.GetValue(x, y, z) & (uint)BlockMask.Road) != 0;

            return random && !structure && !road;
        }

        public void Process(Chunk chunk, ChunkManager chunkManager, int x, int y, int z, BiomeInfo biome, VoxelOctree octree)
        {
            int X = chunk.X * Chunk.CHUNK_SIZE + x;
            int Y = chunk.Y * Chunk.CHUNK_SIZE + y;
            int Z = chunk.Z * Chunk.CHUNK_SIZE + z;

            if (CanPlace(X, Y, Z, x, y, z, biome, octree))
            {
                Vector3I localPos = new Vector3I(x, y - 1, z);
                int variation = (int)(terrainGenerator.Random(localPos) * (rocks.Count - 1));

                terrainGenerator.PlaceStructure(chunk, chunkManager, rocks[variation], localPos);
            }
        }
    }
}
