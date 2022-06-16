using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    class BiomeMap
    {
        HashSet<BiomeData> biomes = new HashSet<BiomeData>();
        FastNoiseLite noise;
        public BiomeMap(FastNoiseLite noise)
        {
            this.noise = noise;
            biomes.Add(new BiomeData(0, 0, 16));
            biomes.Add(new BiomeData(1, 1, 16));
        }

        private float GetNoiseVal(int x, int z)
        {
            double noiseVal = noise.GetNoise(x / 10F, z / 10F);

            return (float)(Math.Max(Math.Min(.5, noiseVal * 10), -.5) + .5);
        }

        public BiomeData GetBiomeData(int x, int z)
        {
            float m = GetNoiseVal(x, z);
            float s = GetNoiseVal(x, z + 1000);
            float h = 16;

            BiomeData outData = new BiomeData();

            outData.mountainess = m;
            outData.mountainSharpness = s;
            outData.baseHeight = h;

            return outData;
        }



        private float Lerp(float d1, float d2, float val)
        {
            val = Math.Max(0, Math.Min(val, 1));

            return d1 * val + d2 * (1 - val);
        }
    }
}
