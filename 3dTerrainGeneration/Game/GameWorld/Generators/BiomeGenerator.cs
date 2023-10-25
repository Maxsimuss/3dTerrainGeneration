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

        [DllImport("Resources/libs/Native.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public extern static BiomeInfo GetBiomeInfo(int x, int z);

        public IntPtr Handle { get; private set; }

        public BiomeGenerator()
        {
            Handle = CreateBiomeGenerator();
        }

        ~BiomeGenerator()
        {
            DeleteBiomeGenerator(Handle);
        }

        public uint GetGrassColor(BiomeInfo biomeInfo)
        {
            throw new NotImplementedException();
        }
    }
}
