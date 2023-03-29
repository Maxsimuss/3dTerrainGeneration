using _3dTerrainGeneration.util;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    internal struct BiomeInfo
    {
        public float Humidity, Temperature, Fertility;

        public BiomeInfo(float temperature, float humidity, float fertility)
        {
            Temperature = temperature;
            Humidity = humidity;
            Fertility = fertility;
        }

        public float DistanceTo(BiomeInfo other)
        {
            float dt = (this.Temperature - other.Temperature) / 30;
            float dh = (this.Humidity - other.Humidity) / 100;
            float df = (this.Fertility - other.Fertility) / 100;

            return MathF.Sqrt(dt * dt + dh * dh + df * df);
        }
    }

    internal class BiomeGenerator
    {
        Dictionary<BiomeInfo, Vector3> colors;
        public BiomeGenerator()
        {
            colors = new Dictionary<BiomeInfo, Vector3>();
            colors.Add(new BiomeInfo(50, 0, 0), new(36, 242, 91));          // desert
            colors.Add(new BiomeInfo(50, 100, 100), new(111, 212, 23));     // jungle
            colors.Add(new BiomeInfo(20, 50, 50), new(102, 252, 78));                          // normal??
            colors.Add(new BiomeInfo(-10, 0, 50), new(72, 224, 214));   //cold
            colors.Add(new BiomeInfo(-30, 0, 50), new(173, 255, 250)); //cold asf
        }

        public BiomeInfo GetBiomeInfo(int X, int Z)
        {
            float Temperature = NoiseUtil.GetPerlin(X - 32898, Z + 29899, 1000) * 30; // -30 to 30 deg
            float Humidity = Math.Clamp(NoiseUtil.GetPerlin(X + 21389, Z - 8937, 1000) * .5f + .5f, 0, 1) * 100; // 0 to 100 %
            float Fertility = Math.Clamp(NoiseUtil.GetPerlin(X - 3874, Z + 3298, 1000) * .5f + .5f, 0, 1) * 100; // 0 to 100 %

            return new BiomeInfo(Temperature, Humidity, Fertility);
        }

        public float TemperatureDropoff(float temp, float Y)
        {
            return temp - Y / 200; //-1 deg per 200 blocks alt.
        }

        public uint GetGrassColor(BiomeInfo biomeInfo)
        {
            float closest = float.MaxValue;
            Vector3 closestColor = new(0, 0, 0);
            foreach (var item in colors)
            {
                float dist = biomeInfo.DistanceTo(item.Key);
                if (dist < closest)
                {
                    closest = dist;
                    closestColor = item.Value;
                }
            }

            return Color.ToInt((byte)closestColor.X, (byte)closestColor.Y, (byte)closestColor.Z);
        }
    }
}
