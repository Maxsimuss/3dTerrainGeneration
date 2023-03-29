using LibNoise.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.util
{
    internal static class NoiseUtil
    {
        private static readonly BevinsValue bevinsNoise = new BevinsValue();
        private static readonly SimplexPerlin perlinNoise = new SimplexPerlin(0, LibNoise.NoiseQuality.Fast);

        public static float OctavePerlinNoise(float x, float y, int octaves, float persistence, float lacunarity, float scale)
        {
            float noise = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;

            // Loop through the number of octaves specified
            for (int octave = 0; octave < octaves; octave++)
            {
                // Calculate the noise value for the current octave
                float noiseValue = perlinNoise.GetValue(x * frequency / scale, y * frequency / scale) * amplitude;

                // Add the noise value to the result and update the maximum value
                noise += noiseValue;
                maxValue += amplitude;

                // Update the frequency and amplitude for the next octave
                frequency *= lacunarity;
                amplitude *= persistence;
            }

            // Normalize the result to the range [-1, 1]
            if (maxValue > 0)
            {
                noise /= maxValue;
            }
            return noise;
        }

        public static float GetPerlin(float x, float z, float scale)
        {
            return perlinNoise.GetValue(x / scale, z / scale);
        }
    }
}
