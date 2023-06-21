using LibNoise.Primitive;
using System;

namespace _3dTerrainGeneration.Engine.Util
{
    internal static class NoiseUtil
    {
        private static readonly BevinsValue bevinsNoise = new BevinsValue();
        private static readonly SimplexPerlin perlinNoise = new SimplexPerlin(0, LibNoise.NoiseQuality.Standard);

        public static float OctavePerlinNoise(float x, float y, int octaves, float persistence, float lacunarity, float scale)
        {
            float noise = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;

            for (int octave = 0; octave < octaves; octave++)
            {
                float noiseValue = perlinNoise.GetValue(x * frequency / scale, y * frequency / scale) * amplitude;

                noise += noiseValue;
                maxValue += amplitude;
                
                frequency *= lacunarity;
                amplitude *= persistence;
            }

            if (maxValue > 0)
            {
                noise /= maxValue;
            }
            return noise;
        }

        public static float GetPerlin(float x, float scale)
        {
            return perlinNoise.GetValue(x / scale);
        }

        public static float GetPerlin(float x, float z, float scale)
        {
            return perlinNoise.GetValue(x / scale, z / scale);
        }
    }
}
