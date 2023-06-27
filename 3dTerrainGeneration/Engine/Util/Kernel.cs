using System.Runtime.InteropServices;

namespace _3dTerrainGeneration.Engine.Util
{
    internal static class Kernel
    {
        [DllImport("kernel32")]
        public static extern int AllocConsole();
    }
}
