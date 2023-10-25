using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Game.GameWorld.Features
{
    internal abstract class NaturalFeature : IFeature
    {
        protected List<Structure> Structures = new List<Structure>();
        private TerrainGenerator terrainGenerator;

        public NaturalFeature(TerrainGenerator terrainGenerator)
        {
            this.terrainGenerator = terrainGenerator;
        }

        public abstract bool Inhabitable(BiomeInfo biome);

        protected abstract bool CanPlace(int X, int Y, int Z, int x, int y, int z, BiomeInfo biome, uint[] octree);

        public void Process(Chunk chunk, ChunkManager chunkManager, int x, int y, int z, BiomeInfo biome, uint[] octree)
        {
            int X = chunk.X * Chunk.CHUNK_SIZE + x;
            int Y = chunk.Y * Chunk.CHUNK_SIZE + y;
            int Z = chunk.Z * Chunk.CHUNK_SIZE + z;

            if (CanPlace(X, Y, Z, x, y, z, biome, octree))
            {
                Vector3I localPos = new Vector3I(x, y, z);
                int variation = (int)(terrainGenerator.Random(localPos) * (Structures.Count - 1));

                terrainGenerator.PlaceStructure(chunk, chunkManager, Structures[variation], localPos);
            }
        }
    }
}
