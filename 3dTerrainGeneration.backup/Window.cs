using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _3dTerrainGeneration.entity;
using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.world;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;

namespace _3dTerrainGeneration
{
    public class Window : GameWindow
    {
        public static Shader _lightingShader;

        private Camera _camera;
        private bool _firstMove = true;

        private Vector2 _lastPos;
        private World world;

        public Window(string title) : base(DisplayDevice.Default.Width, DisplayDevice.Default.Height, new GraphicsMode(new ColorFormat(8), 24, 8, 4), title, GameWindowFlags.Fullscreen, DisplayDevice.Default)
        {
        }

        DepthAttachedFramebuffer framebuffer;

        protected override void OnLoad(EventArgs e)
        {
            GL.Enable(EnableCap.DepthTest);

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.Texture2D);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            framebuffer = new DepthAttachedFramebuffer(Width, Height, PixelInternalFormat.Rgb16f);

            FragmentPass.Init();
            foreach (string shader in Directory.EnumerateFiles("shaders/post"))
            {
                if (shader.EndsWith("D")) continue;
                passes.Add(new FragmentPass(shader, Width, Height));
            }

            passes = passes.OrderBy(p => p.Order).ToList();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            _lightingShader = new Shader("shaders/shader.vert", "shaders/lighting.frag");

            _camera = new Camera(new Vector3(32), Width / (float)Height);

            CursorVisible = false;

            _lightingShader.Use();
            world = new World(_lightingShader);

            base.OnLoad(e);
        }

        List<FragmentPass> passes = new List<FragmentPass>();

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            //Console.WriteLine(player.GetEyePosition());
            if (1D / e.Time < 58)
            {
                //Console.WriteLine(1D / e.Time);
            }
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Texture2D);

            framebuffer.Use();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _lightingShader.Use();

            _lightingShader.SetMatrix4("view", _camera.GetViewMatrix());
            _lightingShader.SetMatrix4("projection", _camera.GetProjectionMatrix());
            _lightingShader.SetVector3("viewPos", _camera.Position);

            //GL.LineWidth(5);
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            world.Render(_lightingShader, _camera, e.Time);
            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            FragmentPass.BeginPostStage();
            Framebuffer buffer = framebuffer;
            foreach (FragmentPass pass in passes)
            {
                buffer = pass.Apply(buffer, .5f, World.renderDist * 2);
            }

            SwapBuffers();

            base.OnRenderFrame(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            var input = Keyboard.GetState();

            if (input.IsKeyDown(Key.Escape))
            {
                Exit();
                Environment.Exit(0);
                return;
            }

            const double sensitivity = 0.15f;
            var mouse = Mouse.GetState();

            if (_firstMove)
            {
                _lastPos = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                float deltaX = mouse.X - _lastPos.X;
                float deltaY = mouse.Y - _lastPos.Y;

                if (!Focused)
                {
                    deltaX = 0;
                    deltaY = 0;
                }
                _lastPos = new Vector2(mouse.X, mouse.Y);

                world.Tick(e.Time);

                world.player.Update(deltaX * sensitivity, -deltaY * sensitivity);
                _camera.Position = world.player.GetEyePosition();
                _camera.Yaw = (float)world.player.GetYaw();
                _camera.Pitch = (float)world.player.GetPitch();
            }

            base.OnUpdateFrame(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (Focused)
            {
                Mouse.SetPosition(X + Width / 2f, Y + Height / 2f);
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            _camera.Fov -= e.DeltaPrecise;
            base.OnMouseWheel(e);
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            _camera.AspectRatio = Width / (float)Height;
            base.OnResize(e);
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);

            GL.DeleteProgram(_lightingShader.Handle);

            base.OnUnload(e);
        }
    }
}