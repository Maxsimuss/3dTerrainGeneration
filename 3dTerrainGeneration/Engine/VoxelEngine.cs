using _3dTerrainGeneration.Engine.Audio;
using _3dTerrainGeneration.Engine.GameWorld.Entity;
using _3dTerrainGeneration.Engine.Graphics;
using _3dTerrainGeneration.Engine.Graphics.UI;
using _3dTerrainGeneration.Engine.Graphics.UI.Screens;
using _3dTerrainGeneration.Engine.Input;
using _3dTerrainGeneration.Engine.Options;
using _3dTerrainGeneration.Engine.Util;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Diagnostics;

namespace _3dTerrainGeneration.Engine
{
    internal class VoxelEngine
    {
        private static readonly int MS_PER_TICK = 50;

        private bool running = true;
        private double tickBalance = 0;
        private Graphics.Window window = null;
        private IGame game;
        private Stopwatch frameTimer = new Stopwatch();

        public UserInputHandler UserInputHandler { get; } = new UserInputHandler();

        public VoxelEngine(IGame game)
        {
            this.game = game;

            window = new Graphics.Window();
            window.Resize += Window_Resize;
            window.Context.MakeCurrent();

            OptionManager.Instance.OnOptionsChanged += (category, name) =>
            {
                if (category == "Audio" && name == "Volume")
                {
                    AudioEngine.Instance.Volume = (float)OptionManager.Instance["Audio", "Volume"] / 10;
                }
            };
        }

        private void Window_Resize(ResizeEventArgs obj)
        {
            GraphicsEngine.Instance.Resize(obj.Width, obj.Height);
        }

        public void Run()
        {
            Environment.SetEnvironmentVariable("ALSOFT_LOGLEVEL", "3");
            Kernel.AllocConsole();

            GraphicsEngine.Instance.Game = game;
            game.EntryPoint(this);
            //#if DEBUG
            UIRenderer.Instance.OpenScreen(new DebugHud());
            //#endif

            while (running)
            {
                Update();
            }
        }

        private void Update()
        {
            unsafe
            {
                if (GLFW.WindowShouldClose(window.WindowPtr))
                {
                    running = false;
                }
            }

            double frameTimeMillis = frameTimer.Elapsed.TotalMilliseconds;

            GraphicsEngine.Instance.TickFraction = MathHelper.Clamp((float)tickBalance / MS_PER_TICK + 1, 0, 1);
            tickBalance += frameTimeMillis;
            frameTimer.Restart();


            Render(frameTimeMillis);

            if (tickBalance > 0)
            {
                tickBalance -= MS_PER_TICK;

                Tick();
            }
        }

        private void Tick()
        {
            EntityManager.Instance.Tick();
            game.World.Tick(GraphicsEngine.Instance.Camera.Position);
            AudioEngine.Instance.Tick(GraphicsEngine.Instance.Camera.Position, default, GraphicsEngine.Instance.Camera.Front, MS_PER_TICK);
        }

        private void Render(double frameTimeMillis)
        {
            //swap buffers for previous frame, hiding cpu -> gpu delay
            window.Context.SwapBuffers();

            window.CursorState = UIRenderer.Instance.IsCursorGrabbed() ? CursorState.Grabbed : CursorState.Normal;

            window.ProcessInputEvents();
            GLFW.PollEvents();
            UserInputHandler.HandleInput(window.KeyboardState, window.MouseState);

            GraphicsEngine.Instance.RenderFrame(frameTimeMillis);
        }
    }
}
