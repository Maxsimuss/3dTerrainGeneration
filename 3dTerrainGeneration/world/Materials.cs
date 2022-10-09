using _3dTerrainGeneration.util;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.world
{
    public class Materials
    {
        public static List<uint> Palette = new List<uint>();

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
            if (!Palette.Contains(i))
            {
                if(Palette.Count > 256)
                {
                    throw new ArgumentOutOfRangeException("Palette Size");
                }
                Palette.Add(i);
            }

            return (byte)(Palette.IndexOf(i) + 1);
        }
    }
}
