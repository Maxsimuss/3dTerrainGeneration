using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using _3dTerrainGeneration.audio;
using _3dTerrainGeneration.entity;
using _3dTerrainGeneration.gui;
using _3dTerrainGeneration.network;
using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.util;
using _3dTerrainGeneration.world;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;

namespace _3dTerrainGeneration
{
    internal class Window : GameWindow
    {
        public static Window Instance { get; private set; }

        Shader GBufferShader, GBufferInstancedShader, ShadowMapShader, ShadowMapInstancedShader, LightingShader, ShadowShader, 
            FXAA, Final, Bloom, Downsample, Upsample, Motionblur;
        public Shader Sky, Stars;
        DepthAttachedFramebuffer GBuffer, ShadowBuffer0, ShadowBuffer1;
        Framebuffer SourceBuffer, TempBuffer0, BloomBuffer0, BloomBuffer1, HUDBuffer;
        public Framebuffer SkyBuffer, StarBuffer;
        private Camera camera;
        private bool _firstMove = true;

        private Vector2 _lastPos;
        private World world;
        public Network network;
        private FontRenderer FontRenderer;
        public ParticleSystem ParticleSystem;
        public SoundManager SoundManager;

        public Window(int w, int h, GameWindowFlags flag, string title) : base(w, h, new GraphicsMode(new ColorFormat(8, 8, 8, 0)), title, flag, DisplayDevice.Default, 4, 20, GraphicsContextFlags.ForwardCompatible) {
            Location = new System.Drawing.Point(DisplayDevice.Default.Width / 2 - w / 2, DisplayDevice.Default.Height / 2 - h / 2);
            Instance = this;
        }

        int shadowRes = 4096;
        float resolutionScale = 1f;
        float shadowResolutionScale = 1f;

        private void InitBuffers(int w, int h)
        {
            w = (int)(w * resolutionScale);
            h = (int)(h * resolutionScale);

            if(GBuffer != null) GBuffer.Dispose();
            GBuffer = new DepthAttachedFramebuffer(w, h,
                new Texture(w, h, PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent),
                new Texture(w, h, PixelInternalFormat.Rgba8, PixelFormat.Rgba),
                new Texture(w, h, PixelInternalFormat.Rgba8, PixelFormat.Rgba));


            int sr = (int)(shadowRes * shadowResolutionScale);
            if (ShadowBuffer0 != null) ShadowBuffer0.Dispose();
            ShadowBuffer0 = new DepthAttachedFramebuffer(sr, sr, 
                new Texture(sr, sr, PixelInternalFormat.DepthComponent16, PixelFormat.DepthComponent, filtered: true, border: true, mode: TextureCompareMode.CompareRefToTexture));

            if (ShadowBuffer1 != null) ShadowBuffer1.Dispose();
            ShadowBuffer1 = new DepthAttachedFramebuffer(sr, sr,
                new Texture(sr, sr, PixelInternalFormat.DepthComponent16, PixelFormat.DepthComponent, filtered: true, border: true, mode: TextureCompareMode.CompareRefToTexture));


            if (SourceBuffer != null) SourceBuffer.Dispose();
            SourceBuffer = new Framebuffer(w, h, new Texture(w, h, PixelInternalFormat.Rgb16f));

            if (TempBuffer0 != null) TempBuffer0.Dispose();
            TempBuffer0 = new Framebuffer(w, h, new Texture(w, h, PixelInternalFormat.Rgb8));

            if (BloomBuffer0 != null) BloomBuffer0.Dispose();
            BloomBuffer0 = new Framebuffer(w / 4, h / 4, new Texture(w / 4, h / 4, PixelInternalFormat.Rgb16f));

            if (BloomBuffer1 != null) BloomBuffer1.Dispose();
            BloomBuffer1 = new Framebuffer(w / 4, h / 4, new Texture(w / 4, h / 4, PixelInternalFormat.Rgb16f));

            if (SkyBuffer != null) SkyBuffer.Dispose();
            SkyBuffer = new Framebuffer(w / 16, h / 16, new Texture(w / 16, h / 16, PixelInternalFormat.Rgb));

            if (StarBuffer != null) StarBuffer.Dispose();
            StarBuffer = new Framebuffer(w, h, new Texture(w, h, PixelInternalFormat.Rgb));

            if (HUDBuffer != null) HUDBuffer.Dispose();
            HUDBuffer = new Framebuffer(w, h, new Texture(w, h, PixelInternalFormat.Rgba, PixelFormat.Rgba));
        }

        private void InitShaders()
        {
#if DEBUG
            string path = "../../../shaders/";
#else
            string path = "shaders/";
#endif
            GBufferShader = new Shader(path + "gbuffer.vert", path + "gbuffer.frag");
            GBufferInstancedShader = new Shader(path + "gbufferinstanced.vert", path + "gbuffer.frag");
            ShadowMapShader = new Shader(path + "shadowmap.vert", path + "empty.frag");
            ShadowMapInstancedShader = new Shader(path + "shadowmapinstanced.vert", path + "empty.frag");

            ShadowShader = new Shader(path + "post.vert", path + "shadow.frag");
            ShadowShader.SetInt("depthTex", 0);
            ShadowShader.SetInt("normalTex", 2);
            ShadowShader.SetInt("colortex4", 3);
            ShadowShader.SetInt("colortex5", 4);

            LightingShader = new Shader(path + "post.vert", path + "lighting.frag");
            LightingShader.SetInt("depthTex", 0);
            LightingShader.SetInt("colorTex", 1);
            LightingShader.SetInt("normalTex", 2);
            LightingShader.SetInt("shadowTex", 3);
            LightingShader.SetInt("skyTex", 4);
            LightingShader.SetInt("starTex", 5);
            LightingShader.SetInt("shadowMapTex", 6);

            FXAA = new Shader(path + "post.vert", path + "post/fxaa.frag");
            FXAA.SetInt("colortex0", 0);

            Bloom = new Shader(path + "post.vert", path + "post/bloom.frag");
            Bloom.SetInt("colortex0", 0);

            Downsample = new Shader(path + "post.vert", path + "post/downsample.frag");
            Downsample.SetInt("colortex0", 0);

            Upsample = new Shader(path + "post.vert", path + "post/upsample.frag");
            Upsample.SetInt("colortex0", 0);

            Final = new Shader(path + "post.vert", path + "post/final.frag");
            Final.SetInt("colortex0", 0);
            Final.SetInt("colortex1", 1);
            Final.SetInt("colortex2", 2);

            Motionblur = new Shader(path + "post.vert", path + "post/motionblur.frag");
            Motionblur.SetInt("colortex0", 0);
            Motionblur.SetInt("colortex1", 1);

            Sky = new Shader(path + "post.vert", path + "sky.frag");
            Stars = new Shader(path + "post.vert", path + "stars.frag");

            Renderer2D.LoadShader(path);
        }
        int SSBO;

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if(currentScreen != null)
                currentScreen.KeyPress(e.KeyChar);

            base.OnKeyPress(e);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if(e.Key == Key.BackSpace)
            {
                if(currentScreen != null)
                    currentScreen.BackSpacePress();
            }

            base.OnKeyDown(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if(currentScreen != null)
                currentScreen.MouseClicked(e.Position.X / (float)Width * 2 - 1, e.Position.Y / (float)Height * -2 + 1);

            base.OnMouseDown(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            Materials.Init();

            Context.MakeCurrent(WindowInfo);

            GL.Enable(EnableCap.DepthTest);

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.Texture2D);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            InitBuffers(Width, Height);

            FragmentPass.Init();
            Renderer2D.Init();

            camera = new Camera(new Vector3(32), Width / (float)Height);

            InitShaders();

            SoundManager = new SoundManager();
            FontRenderer = new FontRenderer();
            ParticleSystem = new ParticleSystem();
            currentScreen = new LoginScreen(FontRenderer, this);

            world = new World(GBufferShader, new InstancedRenderer());

            SSBO = GL.GenBuffer();

            Task.Run(() =>
            {
                network = new Network(world);
                world.network = network;
            });

            base.OnLoad(e);
        }

        float frameTime = 0;

        Matrix4 prevProj, prevView;
        bool altEnter, pr, mr, psr, msr;

        public static string exception = "";
        private LoginScreen currentScreen;

        public static bool login = false;

        double frameCounter = 0;

        double dT = 1 / 20f;

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            SoundManager.Update(camera, e.Time);

            double frameDelta = Math.Min(frameCounter * 20, 1);
            frameCounter += e.Time;
            FontRenderer.SetAspectRatio(Width / (float)Height);

            if (network == null)
            {
                GL.Viewport(0, 0, Width, Height);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit);

                GL.Enable(EnableCap.Blend);
                GL.Disable(EnableCap.DepthTest);
                FontRenderer.DrawTextWithShadowCentered(0, 0, .05f, "Connecting to the server...");

                SwapBuffers();

                base.OnRenderFrame(e);
                return;
            }

            if (!login)
            {
                GL.Viewport(0, 0, Width, Height);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit);

                GL.Enable(EnableCap.Blend);
                GL.Disable(EnableCap.DepthTest);
                if(currentScreen != null)
                    currentScreen.Render();

                SwapBuffers();

                base.OnRenderFrame(e);
                return;
            }
            currentScreen = null;

            var MouseState = Mouse.GetState();
            if (_firstMove)
            {
                _lastPos = new Vector2(MouseState.X, MouseState.Y);
                _firstMove = false;
            }
            else
            {
                float deltaX = MouseState.X - _lastPos.X;
                float deltaY = MouseState.Y - _lastPos.Y;

                _lastPos = new Vector2(MouseState.X, MouseState.Y);

                if (Focused)
                {
                    world.player.Update(Keyboard.GetState(),
                        deltaX * GameSettings.SENSITIVITY, -deltaY * GameSettings.SENSITIVITY,
                        MouseState.IsButtonDown(MouseButton.Left) && !LMB, MouseState.IsButtonDown(MouseButton.Right) && !RMB, frameDelta);
                    LMB = MouseState.IsButtonDown(MouseButton.Left);
                    RMB = MouseState.IsButtonDown(MouseButton.Right);
                }

                //_camera.Yaw = (float)((world.player.GetYaw() + _camera.Yaw * 3) / 4);
                //_camera.Pitch = (float)((world.player.GetPitch() + _camera.Pitch * 3) / 4);

                camera.Yaw = (float)world.player.GetYaw();
                camera.Pitch = (float)world.player.GetPitch();
            }

            float pitch = (float)camera.Pitch;
            float yaw = (float)camera.Yaw;
            if (Focused)
                rangeTarget += wheel - MouseState.ScrollWheelValue;

            rangeTarget = (float)Math.Clamp(rangeTarget, 4, Math.Sqrt(World.renderDist * 20 * .75));

            int range = 0;
            int maxRange = (int)(Math.Pow(rangeTarget, 2) / 20);
            wheel = MouseState.ScrollWheelValue;

            Vector3 position = world.player.GetEyePosition(frameDelta);
            Vector3 ve = new((float)Math.Cos(MathHelper.DegreesToRadians(yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(pitch)), (float)Math.Sin(MathHelper.DegreesToRadians(pitch)), (float)Math.Sin(MathHelper.DegreesToRadians(yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(pitch)));
            if (rangeTarget >= 8)
            {

                while (range < maxRange && !(
                    world.GetBlockAt(position - ve * range) ||
                    world.GetBlockAt(position - ve * range + new Vector3(0, .5f, 0)) ||
                    world.GetBlockAt(position - ve * range + new Vector3(0, -.5f, 0)) ||
                    world.GetBlockAt(position - ve * range + new Vector3(.5f, 0, 0)) ||
                    world.GetBlockAt(position - ve * range + new Vector3(-.5f, 0, 0)) ||
                    world.GetBlockAt(position - ve * range + new Vector3(0, 0, .5f)) ||
                    world.GetBlockAt(position - ve * range + new Vector3(0, 0, -.5f))
                    ))
                {
                    range++;
                }

                rangeCurrent = range;
                //rangeCurrent = (range + rangeCurrent * 9) / 10;
            }
            else
            {
                rangeCurrent = (0 + rangeCurrent * 9) / 10;
            }

            //rangeCurrent = rangeTarget;

            camera.Position = position - ve * rangeCurrent;

            CursorVisible = false;

#if DEBUG
            if (Focused && Keyboard.GetState().IsKeyDown(Key.R))
            {
                GBufferShader.Dispose();
                GBufferInstancedShader.Dispose();
                ShadowMapShader.Dispose();
                ShadowMapInstancedShader.Dispose();
                LightingShader.Dispose();
                ShadowShader.Dispose();
                FXAA.Dispose();
                Final.Dispose();
                Bloom.Dispose();
                Upsample.Dispose();
                Downsample.Dispose();
                Motionblur.Dispose();
                Sky.Dispose();
                Stars.Dispose();
                InitShaders();
                InitBuffers(Width, Height);
            }
#endif

            if (Focused && !altEnter && Keyboard.GetState().IsKeyDown(Key.AltLeft) && Keyboard.GetState().IsKeyDown(Key.Enter))
            {
                WindowState = (WindowState == WindowState.Fullscreen) ? WindowState.Normal : WindowState.Fullscreen;
            }
            altEnter = Keyboard.GetState().IsKeyDown(Key.AltLeft) && Keyboard.GetState().IsKeyDown(Key.Enter);

            if (Focused && !pr && Keyboard.GetState().IsKeyDown(Key.Plus))
            {
                resolutionScale += .125f;
                InitBuffers(Width, Height);
            }
            pr = Keyboard.GetState().IsKeyDown(Key.Plus);

            if (Focused && !mr && Keyboard.GetState().IsKeyDown(Key.Minus))
            {
                resolutionScale -= .125f;
                InitBuffers(Width, Height);
            }
            mr = Keyboard.GetState().IsKeyDown(Key.Minus);

            if (Focused && !psr && Keyboard.GetState().IsKeyDown(Key.BracketRight))
            {
                shadowResolutionScale += .125f;
                InitBuffers(Width, Height);
            }
            psr = Keyboard.GetState().IsKeyDown(Key.BracketRight);

            if (Focused && !msr && Keyboard.GetState().IsKeyDown(Key.BracketLeft))
            {
                shadowResolutionScale -= .125f;
                InitBuffers(Width, Height);
            }
            msr = Keyboard.GetState().IsKeyDown(Key.BracketLeft);

            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);


            FireBall[] fireBalls = world.entities[TerrainServer.network.EntityType.FireBall].Values.Cast<FireBall>().ToArray();
            float[] data = new float[fireBalls.Length * 4];
            for (int i = 0; i < fireBalls.Length; i++)
            {
                Vector3 vec3 = fireBalls[i].GetPositionInterpolated(frameDelta);
                data[i * 4] = vec3.X;
                data[i * 4 + 1] = vec3.Y;
                data[i * 4 + 2] = vec3.Z;
                data[i * 4 + 3] = fireBalls[i].Radius;
            }


            int chunksShadow = 0;
            // render shadowmap
            float shadowRadiusNear = Chunk.Size / 2f;
            float shadowRadiusFar = World.renderDist;

            world.player.Visible = true;
            Matrix4 matrixFar = RenderShadowmap(ShadowBuffer0, shadowRadiusFar, frameDelta);
            ShadowShader.SetMatrix4("matrix1", matrixFar);
            ShadowShader.SetMatrix4("matrix0", RenderShadowmap(ShadowBuffer1, shadowRadiusNear, frameDelta));
            ShadowShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            ShadowShader.SetFloat("shadowRes", (int)(shadowRes * shadowResolutionScale));
            ShadowShader.SetMatrix4("projection", (camera.GetViewMatrix() * camera.GetProjectionMatrix()).Inverted());


            GL.Viewport(0, 0, GBuffer.Width, GBuffer.Height);
            GBuffer.Use();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            //setting uniforms 700us
            GBufferShader.SetMatrix4("view", camera.GetViewMatrix());
            GBufferShader.SetMatrix4("projection", camera.GetProjectionMatrix());
            GBufferInstancedShader.SetMatrix4("view", camera.GetViewMatrix());
            GBufferInstancedShader.SetMatrix4("projection", camera.GetProjectionMatrix());
            GBufferShader.SetVector3("viewPos", camera.Position);
            GBufferShader.SetVector3("viewDir", camera.Front);

            LightingShader.SetVector3("viewPos", camera.Position);
            LightingShader.SetMatrix4("matrixFar", matrixFar);
            LightingShader.SetVector3("sun.position", world.sunPos);
            LightingShader.SetVector3("sun.color", new Vector3(1));
            LightingShader.SetMatrix4("projection", (camera.GetViewMatrix() * camera.GetProjectionMatrix()).Inverted());
            LightingShader.SetMatrix4("_projection", (camera.GetViewMatrix() * camera.GetProjectionMatrix()));
            LightingShader.SetInt("lightCount", fireBalls.Length);
            LightingShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            LightingShader.SetFloat("timeL", (float)(TimeUtil.Unix() / 5000d % 86400d));
            LightingShader.SetFloat("shadowRes", (int)(shadowRes * shadowResolutionScale));

            float s = MathHelper.Clamp(Vector3.Dot(Vector3.UnitY, world.sunPos), 0, 1);
            float dy = (float)Math.Pow(MathHelper.Clamp(world.sunPos.Y, 0, 1), 1 / 2.2);
            float v = (float)Math.Pow(s / 2f, 2f) * 2.2f / (1 + dy * 5) + dy + .25f;
            Vector3 sky = new Vector3((float)Math.Pow(v, 1.6), (float)Math.Pow(v, 2.2), (float)Math.Pow(v, 2.8)) / 2;

            LightingShader.SetVector3("skyLight", sky);


            FXAA.SetInt("width", GBuffer.Width);
            FXAA.SetInt("height", GBuffer.Height);
            Final.SetFloat("aspect", (float)GBuffer.Width / GBuffer.Height);
            Final.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));


            // render camera world
            world.player.Visible = rangeCurrent > 2;
            int chunks = world.Render(GBufferShader, GBufferInstancedShader, LightingShader, camera, e.Time, frameDelta);


            Matrix4 rot = Matrix4.CreateTranslation(-new Vector3(.5f)) * Matrix4.CreateRotationX((float)(TimeUtil.Unix() / 1000D % Math.PI * 2)) * Matrix4.CreateRotationY((float)(TimeUtil.Unix() / 1000D % Math.PI * 2));

            ParticleSystem.Render(camera, (float)e.Time);

            GL.Disable(EnableCap.Blend);
            FragmentPass.BeginPostStage();

            ShadowShader.SetFloat("shadowRadiusNear", shadowRadiusNear);
            ShadowShader.SetFloat("shadowRadiusFar", shadowRadiusFar);
            FragmentPass.Apply(ShadowShader, TempBuffer0, GBuffer, ShadowBuffer0, ShadowBuffer1);

            GL.Viewport(0, 0, SkyBuffer.Width, SkyBuffer.Height);
            Sky.SetMatrix4("projection", camera.GetProjectionMatrix().Inverted());
            Sky.SetMatrix4("viewMatrix", camera.GetViewMatrix());
            Sky.SetVector3("viewPos", camera.Position);
            Sky.SetVector3("sun_dir", world.sunPos);
            Sky.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 3600d));
            FragmentPass.Apply(Sky, SkyBuffer);

            GL.Viewport(0, 0, StarBuffer.Width, StarBuffer.Height);
            Stars.SetMatrix4("projection", camera.GetProjectionMatrix().Inverted());
            Stars.SetMatrix4("viewMatrix", camera.GetViewMatrix());
            Stars.SetVector3("sun_dir", world.sunPos);
            Stars.SetFloat("time", (float)(TimeUtil.Unix() / 100000d % 3600d));
            FragmentPass.Apply(Stars, StarBuffer);


            GL.Viewport(0, 0, GBuffer.Width, GBuffer.Height);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, SSBO);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(float) * data.Length, data, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, SSBO);
            FragmentPass.Apply(LightingShader, SourceBuffer, GBuffer, TempBuffer0, SkyBuffer, StarBuffer, ShadowBuffer0);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            GL.Viewport(0, 0, BloomBuffer0.Width, BloomBuffer0.Height);
            FragmentPass.Apply(Bloom, BloomBuffer0, SourceBuffer);

            Downsample.SetFloat("tw", 1f / BloomBuffer0.Width);
            Downsample.SetFloat("th", 1f / BloomBuffer0.Height);
            Upsample.SetFloat("tw", 1f / BloomBuffer0.Width);
            Upsample.SetFloat("th", 1f / BloomBuffer0.Height);

            Downsample.SetFloat("radius", .001f * Width * resolutionScale);
            FragmentPass.Apply(Downsample, BloomBuffer1, BloomBuffer0);
            Downsample.SetFloat("radius", .002f * Width * resolutionScale);
            FragmentPass.Apply(Downsample, BloomBuffer0, BloomBuffer1);
            Upsample.SetFloat("radius", .001f * Width * resolutionScale);
            FragmentPass.Apply(Upsample, BloomBuffer1, BloomBuffer0);
            Upsample.SetFloat("radius", .002f * Width * resolutionScale);
            FragmentPass.Apply(Upsample, BloomBuffer0, BloomBuffer1);

            GL.Viewport(0, 0, Width, Height);

            HUDBuffer.Use();
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

            frameTime = (float)(e.Time + frameTime * 29) / 30;
            GL.Enable(EnableCap.Blend);
            FontRenderer.DrawTextWithShadowCentered(0, 1f - .06f * Width / Height, .05f, string.Format("{0:00} FPS", (int)(1 / frameTime)));

            FontRenderer.DrawTextWithShadow(-1 + .025f, 1f - .075f * FontRenderer.aspectRatio, .025f, string.Format("Day {0} {1:00}:{2:00}", (int)(world.time / 1000 / 1440), (int)(world.time / 1000 / 1440 * 24 % 24), (int)(world.time / 1000 / 1440 * 24 * 60 % 60)));
            FontRenderer.DrawTextWithShadow(-1 + .025f, 1f - .125f * FontRenderer.aspectRatio, .025f, "Lands of Pososich");

            Renderer2D.DrawRect(-.3f, -.825f, -.0125f, -.775f, new(186 / 255f, 43 / 255f, 43 / 255f, 1));
            FontRenderer.DrawTextWithShadowCentered((-.3f - .0125f) / 2f, -.8f, .0125f, string.Format("{0:0.0} / {1} HP", world.player.health, world.player.maxHealth));

            Renderer2D.DrawRect(.0125f, -.825f, .3f, -.775f, new(52 / 255f, 147 / 255f, 235 / 255f, 1));
            FontRenderer.DrawTextWithShadowCentered((.3f + .0125f) / 2f, -.8f, .0125f, "1000 / 1000 MP");

            Renderer2D.DrawRect(-.3f, -.85f, .3f, -1, new(0, 0, 0, .5f));

            FontRenderer.DrawTextWithShadow(-.95f, -.95f, .02f, "Type here to chat.");
            Renderer2D.DrawRect(-.975f, -.975f, -.325f, -.97f, new(1, 1, 1, 1));

            FontRenderer.DrawTextWithShadowCentered(0, 0, .05f, exception, new(1, 0, 0, 1));

            FragmentPass.BeginPostStage();
            FragmentPass.Apply(Final, null, SourceBuffer, BloomBuffer0, HUDBuffer);

            //Motionblur.SetMatrix4("gbufferPreviousProjection", prevProj);
            //Motionblur.SetMatrix4("gbufferPreviousModelView", prevView);

            //prevProj = _camera.GetProjectionMatrix();
            //prevView = _camera.GetViewMatrix();
            //Motionblur.SetMatrix4("gbufferProjectionInverse", _camera.GetProjectionMatrix().Inverted());
            //Motionblur.SetMatrix4("gbufferModelViewInverse", _camera.GetViewMatrix().Inverted());
            //FragmentPass.Apply(Motionblur, null, StarBuffer, GBuffer);

            //FragmentPass.Apply(FXAA, TempBuffer0, TempBuffer1);
            //FragmentPass.Apply(FXAA, null, TempBuffer0);

            SwapBuffers();


            base.OnRenderFrame(e);
        }

        private Matrix4 RenderShadowmap(DepthAttachedFramebuffer shadowBuffer, float r, double frameDelta)
        {
            Vector3 lightPos = world.sunPos * World.renderDist * 1.41f;

            Vector3 cameraPos = new Vector3((int)camera.Position.X / 4 * 4, (int)camera.Position.Y / 8 * 8, (int)camera.Position.Z / 4 * 4);
            Matrix4 lsm = Matrix4.LookAt(lightPos + cameraPos, cameraPos, new Vector3(1, 0, 0)) *
                Matrix4.CreateOrthographicOffCenter(-r, r, -r, r, .0f, World.renderDist * 3);
            ShadowMapShader.SetMatrix4("lightSpaceMatrix", lsm);
            ShadowMapInstancedShader.SetMatrix4("lightSpaceMatrix", lsm);

            GL.Viewport(0, 0, shadowBuffer.Width, shadowBuffer.Height);
            shadowBuffer.Use();
            GL.Clear(ClearBufferMask.DepthBufferBit);
            world.RenderWorld(camera.Position, World.renderDist / Chunk.Size + 1.4f, ShadowMapShader, ShadowMapInstancedShader, frameDelta);

            return lsm;
        }

        bool LMB, RMB;
        float rangeCurrent = 0;
        float rangeTarget = 8;
        float wheel = 0;

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            frameCounter = 0;
            if (network == null)
            {
                base.OnUpdateFrame(e);

                return;
            }

            network.Update(dT);

            if(!login)
            {
                return;
            }
            world.Tick(dT);

            camera.Velocity = new((float)world.player.motionX, (float)world.player.motionY, (float)world.player.motionZ);

            var MouseState = Mouse.GetState();
            if (Keyboard.GetState().IsKeyDown(Key.Escape) && Focused)
            {
                Close();
                Environment.Exit(0);
                return;
            }


            CursorGrabbed = Focused;

            base.OnUpdateFrame(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
         
            camera.AspectRatio = Width / (float)Height;
            InitBuffers(Width, Height);
        }

        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);
        }
    }
}