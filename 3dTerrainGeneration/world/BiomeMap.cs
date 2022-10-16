using System;
using System.Collections.Generic;

namespace _3dTerrainGeneration.world
{
    class BiomeMap
    {
        HashSet<BiomeData> biomes = new HashSet<BiomeData>();



        private float Lerp(float d1, float d2, float val)
        {
            val = Math.Max(0, Math.Min(val, 1));

            return d1 * val + d2 * (1 - val);
        }
    }
}
