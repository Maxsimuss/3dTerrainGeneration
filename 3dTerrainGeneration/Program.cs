using _3dTerrainGeneration.util;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
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
            RunGame();
        }

        private static void RunGame()
        {
            Environment.SetEnvironmentVariable("ALSOFT_LOGLEVEL", "3");

#if DEBUG
            bool fullscreen = false;
#else
            bool fullscreen = false;
#endif
            //int w = fullscreen ? Default.Width : DisplayDevice.Default.Width / 4;
            //int h = fullscreen ? DisplayDevice.Default.Height : DisplayDevice.Default.Height / 4;
            //GameWindowFlags flag = fullscreen ? GameWindowFlags.Fullscreen : GameWindowFlags.Default;

            GameWindowSettings gs = new GameWindowSettings()
            {
                UpdateFrequency = 500,
                RenderFrequency = 500,
            };
            NativeWindowSettings ns = new NativeWindowSettings()
            {
                APIVersion = new Version(4, 6),
            };

            Window window = new Window(1700, 1000, gs, ns);

            if (!fullscreen)
            {
                //AllocConsole();
            }

            window.Run();
        }
    }
}
