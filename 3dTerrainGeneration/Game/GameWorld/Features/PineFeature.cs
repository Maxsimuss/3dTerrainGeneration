using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace _3dTerrainGeneration.Game.GameWorld.Features
{
    internal class PineFeature : NaturalFeature
    {
        public PineFeature(TerrainGenerator terrainGenerator) : base(terrainGenerator)
        {
            Structures.Add(new ImportedStructure("Natural/Pine/pine0.vox"));
            Structures.Add(new ImportedStructure("Natural/Pine/pine1.vox"));
            Structures.Add(new ImportedStructure("Natural/Pine/pine2.vox"));
            Structures.Add(new ImportedStructure("Natural/Pine/pine3.vox"));
        }

        public override bool Inhabitable(BiomeInfo biome)
        {
            bool temperature = biome.Temperature > -30 && biome.Temperature < 15;
            bool humidity = biome.Humidity > 20;
            bool fertility = biome.Fertility > 10;

            return temperature && humidity && fertility;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool CanPlace(int X, int Y, int Z, int x, int y, int z, BiomeInfo biome, uint[] octree)
        {
            if (y < 1 || octree[y] != 0)
            {
                return false;
            }

            bool isFertile = (octree[y - 1] & (uint)BlockMask.Fertile) != 0;
            bool road = (octree[y] & (uint)BlockMask.Road) != 0;
            bool random = NoiseUtil.GetPerlin(X, Z + 6463, 2) > .98f;

            return isFertile && !road && random;
        }
    }
}
