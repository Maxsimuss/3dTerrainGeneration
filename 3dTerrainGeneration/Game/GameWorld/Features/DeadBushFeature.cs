using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using _3dTerrainGeneration.Game.GameWorld.Structures;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace _3dTerrainGeneration.Game.GameWorld.Features
{
    internal class DeadBushFeature : NaturalFeature
    {
        public DeadBushFeature(TerrainGenerator terrainGenerator) : base(terrainGenerator)
        {
            Structures.Add(new ImportedStructure("Natural/DeadBush/deadbush0.vox"));
            Structures.Add(new ImportedStructure("Natural/DeadBush/deadbush1.vox"));
            Structures.Add(new ImportedStructure("Natural/DeadBush/deadbush2.vox"));
        }

        public override bool Inhabitable(BiomeInfo biome)
        {
            bool humidity = biome.Humidity < 10;
            bool fertility = biome.Fertility < 20;

            return humidity || fertility;
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
            bool random = NoiseUtil.GetPerlin(X, Z + 4387, 2) > .99f;

            return isFertile && !road && random;
        }
    }
}
