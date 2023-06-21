using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Util
{
    internal static class Kernel
    {
        [DllImport("kernel32")]
        public static extern int AllocConsole();
    }
}
