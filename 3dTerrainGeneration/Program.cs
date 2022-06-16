using OpenTK;
using System;
using System.Runtime.InteropServices;

namespace _3dTerrainGeneration
{
    public static class Program
    {

        [DllImport("kernel32")]
        static extern int AllocConsole();

        private static void Main()
        {
            Environment.SetEnvironmentVariable("ALSOFT_LOGLEVEL", "3");

#if DEBUG
            bool fullscreen = false;
#else
            bool fullscreen = false;
#endif
            int w = fullscreen ? DisplayDevice.Default.Width : DisplayDevice.Default.Width / 4;
            int h = fullscreen ? DisplayDevice.Default.Height : DisplayDevice.Default.Height / 4;
            GameWindowFlags flag = fullscreen ? GameWindowFlags.Fullscreen : GameWindowFlags.Default;
            Window window = new Window(w, h, flag, "gaem");

            if(!fullscreen)
            {
                AllocConsole();
            }

            window.Run(20, 0);
        }
    }
}
