using _3dTerrainGeneration.Engine.Graphics;
using _3dTerrainGeneration.Engine.Options;
using _3dTerrainGeneration.Engine.Util;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Runtime.InteropServices;
using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.Physics;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using _3dTerrainGeneration.Engine.Graphics.UI;
using _3dTerrainGeneration.Engine.Graphics.UI.Screens;

namespace _3dTerrainGeneration.Engine
{
    internal class VoxelEngine
    {
        private bool running = true;
        private double tickBalance = 0;
        private Graphics.Window window = null;
        private IGame game;
        private Stopwatch frameTimer = new Stopwatch();

        public VoxelEngine(IGame game)
        {
            this.game = game;

            window = new Graphics.Window();
            window.Resize += Window_Resize;
            window.Context.MakeCurrent();
        }

        private void Window_Resize(OpenTK.Windowing.Common.ResizeEventArgs obj)
        {
            GraphicsEngine.Instance.Resize(obj.Width, obj.Height);
        }

        public void Run()
        {
            Environment.SetEnvironmentVariable("ALSOFT_LOGLEVEL", "3");
            Kernel.AllocConsole();

            GraphicsEngine.Instance.Game = game;
            game.EntryPoint();
#if DEBUG
            UIRenderer.Instance.OpenScreen(new DebugHud());
#endif

            while (running)
            {
                unsafe
                {
                    if (GLFW.WindowShouldClose(window.WindowPtr))
                    {
                        running = false;
                    }
                }

                window.ProcessInputEvents();
                GLFW.PollEvents();

                long frameTimeMillis = frameTimer.ElapsedMilliseconds;
                tickBalance += frameTimeMillis;
                frameTimer.Restart();
                GraphicsEngine.Instance.RenderFrame(frameTimeMillis);

                if (tickBalance > 0)
                {
                    tickBalance -= 50;

                    game.World.Tick(GraphicsEngine.Instance.Camera.Position);
                }

                if(window.KeyboardState.IsKeyDown(Keys.R))
                {
                    GraphicsEngine.Instance.Reload();
                }

                window.Context.SwapBuffers();
            }
        }
    }
}
