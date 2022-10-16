using _3dTerrainGeneration.util;
using System;
using System.Collections.Generic;

namespace _3dTerrainGeneration.world
{
    public class Materials
    {
        private static List<uint> Palette = new List<uint>();

        public static void Init()
        {

        }

        public static byte IdOf(byte r, byte g, byte b)
        {
            uint i = Color.ToInt(r, g, b);
            if (!Palette.Contains(i))
            {
                if (Palette.Count > 256)
                {
                    throw new ArgumentOutOfRangeException("Palette Size");
                }
                Palette.Add(i);
            }

            return (byte)(Palette.IndexOf(i) + 1);
        }

        public static byte IdOf(uint i)
        {
            i = (i >> 16 & 0xff) / 85 * 85 << 16 | (i >> 8 & 0xff) / 36 * 36 << 8 | (i & 0xff) / 85 * 85;

            if (!Palette.Contains(i))
            {
                if (Palette.Count > 256)
                {
                    throw new ArgumentOutOfRangeException("Palette Size");
                }
                Palette.Add(i);
            }

            return (byte)(Palette.IndexOf(i) + 1);
        }

        public static uint Get(byte id)
        {
            return Palette[id];
        }
    }
}
