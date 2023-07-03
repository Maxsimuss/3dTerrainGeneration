using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;

namespace _3dTerrainGeneration.Game.GameWorld.Features
{
    internal interface IFeature
    {
        public bool Inhabitable(BiomeInfo biome);

        public void Process(Chunk chunk, ChunkManager chunkManager, int x, int y, int z, BiomeInfo biome, uint[] octree);
    }
}
