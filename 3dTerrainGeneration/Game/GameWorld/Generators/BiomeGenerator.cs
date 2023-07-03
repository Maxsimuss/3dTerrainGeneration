using _3dTerrainGeneration.Engine.Util;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

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
        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private extern static IntPtr CreateBiomeGenerator();

        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private extern static void DeleteBiomeGenerator(IntPtr biomeGen);

        public IntPtr Handle { get; private set; }

        public BiomeGenerator()
        {
            Handle = CreateBiomeGenerator();
        }

        ~BiomeGenerator()
        {
            DeleteBiomeGenerator(Handle);
        }

        public BiomeInfo GetBiomeInfo(int X, int Z)
        {
            float Temperature = NoiseUtil.GetPerlin(X - 32898, Z + 29899, 10000) * 40 + 10; // -30 to 50 deg
            float Humidity = Math.Clamp(NoiseUtil.GetPerlin(X + 21389, Z - 8937, 10000) * .5f + .5f, 0, 1) * 100; // 0 to 100 %
            float Fertility = Math.Clamp(NoiseUtil.GetPerlin(X - 3874, Z + 3298, 10000) * .5f + .5f, 0, 1) * 100; // 0 to 100 %

            return new BiomeInfo(Temperature, Humidity, Fertility);
        }

        public uint GetGrassColor(BiomeInfo biomeInfo)
        {
            throw new NotImplementedException();
        }
    }
}
