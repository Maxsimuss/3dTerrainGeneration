using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace _3dTerrainGeneration.Game.GameWorld.Features
{
    internal class DeadTreeFeature : NaturalFeature
    {
        public DeadTreeFeature(TerrainGenerator terrainGenerator) : base(terrainGenerator)
        {
            Structures.Add(new ImportedStructure("Natural/DeadTree/deadtree0.vox"));
            Structures.Add(new ImportedStructure("Natural/DeadTree/deadtree1.vox"));
            Structures.Add(new ImportedStructure("Natural/DeadTree/deadtree2.vox"));
        }

        public override bool Inhabitable(BiomeInfo biome)
        {
            bool extremeTemperature = biome.Temperature < -10;
            bool temperature = biome.Temperature < 20;
            bool humidity = biome.Humidity < 30;
            bool fertility = biome.Fertility < 40;

            return (((temperature && fertility) || extremeTemperature) && humidity) || biome.Fertility < 20;
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
            bool random = NoiseUtil.GetPerlin(X, Z + 2084, 2) > .999f;

            return isFertile && !road && random;
        }
    }
}
