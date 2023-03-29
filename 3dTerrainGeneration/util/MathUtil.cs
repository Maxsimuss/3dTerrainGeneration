using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.util
{
    public static class MathUtil
    {
        //https://stackoverflow.com/a/1082938
        public static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
