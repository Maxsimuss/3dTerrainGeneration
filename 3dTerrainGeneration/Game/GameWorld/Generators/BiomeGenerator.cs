﻿using _3dTerrainGeneration.Engine.Util;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace _3dTerrainGeneration.Game.GameWorld.Generators
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
            float dt = (Temperature - other.Temperature) / 30;
            float dh = (Humidity - other.Humidity) / 100;
            float df = (Fertility - other.Fertility) / 100;

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
            float Temperature = NoiseUtil.GetPerlin(X - 32898, Z + 29899, 10000) * 40 + 10; // -30 to 50 deg
            float Humidity = Math.Clamp(NoiseUtil.GetPerlin(X + 21389, Z - 8937, 10000) * .5f + .5f, 0, 1) * 100; // 0 to 100 %
            float Fertility = Math.Clamp(NoiseUtil.GetPerlin(X - 3874, Z + 3298, 10000) * .5f + .5f, 0, 1) * 100; // 0 to 100 %

            return new BiomeInfo(Temperature, Humidity, Fertility);
        }

        public float TemperatureDropoff(float temp, float Y)
        {
            return temp - Y / 200; //-1 deg per 200 blocks alt.
        }

        public uint GetGrassColor(BiomeInfo biomeInfo)
        {
            float total = 0;
            Vector3 color = new(0, 0, 0);
            foreach (var item in colors)
            {
                float dist = 1 / biomeInfo.DistanceTo(item.Key);
                total += dist;
                color += item.Value * dist;
            }

            color /= total;

            return Color.ToInt((byte)color.X, (byte)color.Y, (byte)color.Z);
        }
    }
}
