using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Game.GameWorld.Features
{
    internal interface IFeature
    {
        public bool Inhabitable(BiomeInfo biome);

        public void Process(Chunk chunk, ConcurrentDictionary<Vector3I, Chunk> chunks, HashSet<Chunk> modifiedChunks, int x, int y, int z, BiomeInfo biome, VoxelOctree octree);
    }
}
