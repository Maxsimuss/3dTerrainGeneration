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
        public static List<uint> pallette = new List<uint>();

        public static void Init()
        {

        }

        public static byte IdOf(byte r, byte g, byte b)
        {
            uint i = Color.ToInt(r, g, b);

            if(!pallette.Contains(i))
            {
                pallette.Add(i);
            }

            return (byte)(pallette.IndexOf(i) + 1);
        }

        public static byte IdOf(uint i)
        {
            if (!pallette.Contains(i))
            {
                pallette.Add(i);
            }

            return (byte)(pallette.IndexOf(i) + 1);
        }
    }
}
