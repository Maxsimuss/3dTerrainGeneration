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
    internal unsafe class Window : GameWindow
    {
        public static Window Instance { get; private set; }

        public FragmentShader Sky, Stars;
        private FragmentShader GBufferShader, GBufferInstancedShader, ShadowMapShader, ShadowMapInstancedShader, LightingShader, VolumetricsShader, 
            ShadowShader, TAA, Final3D, Final2D, BloomShader, DownsampleShader, UpsampleShader, Motionblur, TonemappingShader, OcclusionShader;
        private ComputeShader LuminanceCompute;

        private DepthAttachedFramebuffer GBuffer, ShadowBuffer0, ShadowBuffer1;
        
        private Framebuffer SourceBuffer0, SourceBuffer1, VolumetricBuffer, TempBuffer0, BloomBuffer0, BloomBuffer1, HUDBuffer, 
            OcclusionBuffer0, OcclusionBuffer1, TAABuffer0, TAABuffer1;
        public Framebuffer SkyBuffer, StarBuffer;

        int FireBallSSBO, LuminanceSSBO;

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
        bool pingPong = true;

        private void InitBuffers(int w, int h)
        {
            w = (int)(w * resolutionScale);
            h = (int)(h * resolutionScale);

            if(GBuffer != null) GBuffer.Dispose();
            GBuffer = new DepthAttachedFramebuffer(w, h,
                new Texture(w, h, PixelInternalFormat.DepthComponent32f, PixelFormat.DepthComponent),
                new Texture(w, h, PixelInternalFormat.Rgba8, PixelFormat.Rgba),
                new Texture(w, h, PixelInternalFormat.Rgba8, PixelFormat.Rgba));


            int sr = (int)(shadowRes * shadowResolutionScale);
            if (ShadowBuffer0 != null) ShadowBuffer0.Dispose();
            ShadowBuffer0 = new DepthAttachedFramebuffer(sr, sr, 
                new Texture(sr, sr, PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent, filtered: true, border: true, mode: TextureCompareMode.None));

            if (ShadowBuffer1 != null) ShadowBuffer1.Dispose();
            ShadowBuffer1 = new DepthAttachedFramebuffer(sr, sr,
                new Texture(sr, sr, PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent, filtered: true, border: true, mode: TextureCompareMode.None));


            if (SourceBuffer0 != null) SourceBuffer0.Dispose();
            SourceBuffer0 = new Framebuffer(w, h, new Texture(w, h, PixelInternalFormat.Rgba16f, Mipmapped: true));

            if (SourceBuffer1 != null) SourceBuffer1.Dispose();
            SourceBuffer1 = new Framebuffer(w, h, new Texture(w, h, PixelInternalFormat.Rgba16f));

            if (VolumetricBuffer != null) VolumetricBuffer.Dispose();
            VolumetricBuffer = new Framebuffer(w / 2, h / 2, new Texture(w / 2, h / 2, PixelInternalFormat.R8, PixelFormat.Red));

            if (TempBuffer0 != null) TempBuffer0.Dispose();
            TempBuffer0 = new Framebuffer(w, h, new Texture(w, h, PixelInternalFormat.Rgb8));

            if (BloomBuffer0 != null) BloomBuffer0.Dispose();
            BloomBuffer0 = new Framebuffer(w / 4, h / 4, new Texture(w / 4, h / 4, PixelInternalFormat.Rgb16f));

            if (BloomBuffer1 != null) BloomBuffer1.Dispose();
            BloomBuffer1 = new Framebuffer(w / 4, h / 4, new Texture(w / 4, h / 4, PixelInternalFormat.Rgb16f));

            if (SkyBuffer != null) SkyBuffer.Dispose();
            SkyBuffer = new Framebuffer(w / 16, h / 16, new Texture(w / 16, h / 16, PixelInternalFormat.Rgb16f));

            if (StarBuffer != null) StarBuffer.Dispose();
            StarBuffer = new Framebuffer(w, h, new Texture(w, h, PixelInternalFormat.Rgb));

            if (HUDBuffer != null) HUDBuffer.Dispose();
            HUDBuffer = new Framebuffer(Width, Height, new Texture(Width, Height, PixelInternalFormat.Rgba, PixelFormat.Rgba));

            if (OcclusionBuffer0 != null) OcclusionBuffer0.Dispose();
            OcclusionBuffer0 = new Framebuffer(w, h, new Texture(w, h, PixelInternalFormat.R8, PixelFormat.Red));

            if (OcclusionBuffer1 != null) OcclusionBuffer1.Dispose();
            OcclusionBuffer1 = new Framebuffer(w, h, new Texture(w, h, PixelInternalFormat.R8, PixelFormat.Red));

            if (TAABuffer0 != null) TAABuffer0.Dispose();
            TAABuffer0 = new Framebuffer(w, h, new Texture(w, h, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, border: true));
            if (TAABuffer1 != null) TAABuffer1.Dispose();
            TAABuffer1 = new Framebuffer(w, h, new Texture(w, h, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, border: true));
        }

        private void InitShaders()
        {
#if DEBUG
            string path = "../../../shaders/";
#else
            string path = "shaders/";
#endif
            GBufferShader = new FragmentShader(path + "gbuffer.vert", path + "gbuffer.frag");
            GBufferInstancedShader = new FragmentShader(path + "gbufferinstanced.vert", path + "gbuffer.frag");
            ShadowMapShader = new FragmentShader(path + "shadowmap.vert", path + "empty.frag");
            ShadowMapInstancedShader = new FragmentShader(path + "shadowmapinstanced.vert", path + "empty.frag");

            ShadowShader = new FragmentShader(path + "post.vert", path + "shadow.frag");
            ShadowShader.SetInt("depthTex", 0);
            ShadowShader.SetInt("normalTex", 2);
            ShadowShader.SetInt("colortex4", 3);
            ShadowShader.SetInt("colortex5", 4);

            LightingShader = new FragmentShader(path + "post.vert", path + "lighting.frag");
            LightingShader.SetInt("depthTex", 0);
            LightingShader.SetInt("colorTex", 1);
            LightingShader.SetInt("normalTex", 2);
            LightingShader.SetInt("shadowTex", 3);
            LightingShader.SetInt("skyTex", 4);
            LightingShader.SetInt("starTex", 5);
            LightingShader.SetInt("fogTex", 6);
            LightingShader.SetInt("occlusionTex", 7);

            OcclusionShader = new FragmentShader(path + "post.vert", path + "occlusion.frag");
            OcclusionShader.SetInt("depthTex", 0);
            OcclusionShader.SetInt("normalTex", 2);
            OcclusionShader.SetInt("occlusionTex", 3);

            VolumetricsShader = new FragmentShader(path + "post.vert", path + "volumetrics.frag");
            VolumetricsShader.SetInt("depthTex", 0);
            VolumetricsShader.SetInt("normalTex", 2);
            VolumetricsShader.SetInt("shadowMapTex", 3);

            TAA = new FragmentShader(path + "post.vert", path + "post/taa.frag");
            TAA.SetInt("depthTex", 0);
            TAA.SetInt("colorTex0", 3);
            TAA.SetInt("colorTex1", 4);

            BloomShader = new FragmentShader(path + "post.vert", path + "post/bloom.frag");
            BloomShader.SetInt("colortex0", 0);

            DownsampleShader = new FragmentShader(path + "post.vert", path + "post/downsample.frag");
            DownsampleShader.SetInt("colortex0", 0);

            UpsampleShader = new FragmentShader(path + "post.vert", path + "post/upsample.frag");
            UpsampleShader.SetInt("colortex0", 0);

            Final3D = new FragmentShader(path + "post.vert", path + "post/final3d.frag");
            Final3D.SetInt("colortex0", 0);
            Final3D.SetInt("colortex1", 1);

            Final2D = new FragmentShader(path + "post.vert", path + "post/final2d.frag");
            Final2D.SetInt("colortex0", 0);
            Final2D.SetInt("colortex1", 1);

            Motionblur = new FragmentShader(path + "post.vert", path + "post/motionblur.frag");
            Motionblur.SetInt("colortex0", 0);
            Motionblur.SetInt("colortex1", 1);

            TonemappingShader = new FragmentShader(path + "post.vert", path + "tonemapping.frag");
            TonemappingShader.SetInt("colorTex", 0);

            Sky = new FragmentShader(path + "post.vert", path + "sky.frag");
            Stars = new FragmentShader(path + "post.vert", path + "stars.frag");

            LuminanceCompute = new ComputeShader(path + "luminance.comp");

            Renderer2D.LoadShader(path);
        }

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

        private int farlandVBO, farlandVAO, farlandLen;

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

            world = new World(new InstancedRenderer());

            FireBallSSBO = GL.GenBuffer();
            LuminanceSSBO = GL.GenBuffer();

            farlandVBO = GL.GenBuffer();
            farlandVAO = GL.GenVertexArray();
            GL.BindBuffer(BufferTarget.ArrayBuffer, farlandVBO);
            GL.BindVertexArray(farlandVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, farlandVBO);
            var dataLocation = GBufferShader.GetAttribLocation("aData");
            GL.EnableVertexAttribArray(dataLocation);
            GL.VertexAttribPointer(dataLocation, 4, VertexAttribPointerType.UnsignedShort, false, 4 * sizeof(ushort), 0);

            MeshData meshData = new MeshData();
            for (int x = 0; x < 64; x++)
            {
                int X = x * 64 - 2048;
                for (int z = 0; z < 64; z++)
                {
                    int Z = z * 64 - 2048;

                    float h = GetHeight(X, Z);
                    
                    float temp = Chunk.smoothstep(0, 1, Chunk.GetPerlin(X, Z, .00025f) - (h / 512f));
                    if (temp < .16)
                    {
                        meshData.SetBlock(x, 0, z, Color.ToInt(255, 255, 255));
                    }
                    else
                    {
                        temp -= .16f;
                        temp /= 1f - .16f;

                        float humidity = Chunk.smoothstep(0, 1, Chunk.GetPerlin(X + 12312, Z - 124124, .00025f));

                        meshData.SetBlock(x, 0, z, Color.HsvToRgb(
                                150 - (byte)((byte)(temp * 8) * 13),
                                166 + (byte)((byte)(humidity * 4) * 16),
                                220 - (byte)((byte)(humidity * 4) * 15)
                            )
                        );
                    }
                }
            }

            float GetHeight(int x, int z)
            {
                return (float)Math.Pow(Chunk.OcataveNoise(x, z, .0005f / 4, 8) * 1.2, 7) * Chunk.GetPerlin(x, z, .0005f / 4) * 255 * 4;
            }

            ushort[] mesh = meshData.Mesh(0);
            GL.BufferData(BufferTarget.ArrayBuffer, mesh.Length * sizeof(ushort), mesh, BufferUsageHint.StaticDraw);
            farlandLen = mesh.Length / 4;

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

        public static string message = "";
        private LoginScreen currentScreen;

        public static bool login = false;

        double frameCounter = 0;

        double dT = 1 / 20f;

        float luma = 2;

        Vector3[] offsets = new Vector3[] { new(-1, 1, 0), new(0, 1, 0), new(1, 1, 0),
                                            new(-1, 0, 0), new(0, 0, 0), new(1, 0, 0),
                                            new(-1, -1, 0), new(0, -1, 0), new(1, -1, 0)};
        int counter = 0;
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            counter++;
            pingPong = !pingPong;

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
                OcclusionShader.Dispose();
                VolumetricsShader.Dispose();
                ShadowShader.Dispose();
                TAA.Dispose();
                Final3D.Dispose();
                BloomShader.Dispose();
                UpsampleShader.Dispose();
                DownsampleShader.Dispose();
                Motionblur.Dispose();
                Sky.Dispose();
                Stars.Dispose();
                TonemappingShader.Dispose();
                LuminanceCompute.Dispose();
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
            float shadowRadiusNear = 64f;
            float shadowRadiusFar = 1024;

            Vector3 randVec = offsets[counter % 9] * new Vector3(1f / GBuffer.Width, 1f / GBuffer.Height, 0f);
            randVec = new Vector3(0);
            world.player.Visible = true;
            ShadowMapShader.SetVector3("rand", randVec);
            ShadowMapShader.SetMatrix4("view", camera.GetViewMatrix());
            ShadowMapShader.SetMatrix4("projection", camera.GetProjectionMatrix());
            //Matrix4 matrixFar = RenderShadowmap(ShadowBuffer0, shadowRadiusFar, frameDelta);
            //ShadowShader.SetMatrix4("matrix1", matrixFar);
            //ShadowShader.SetMatrix4("matrix0", RenderShadowmap(ShadowBuffer1, shadowRadiusNear, frameDelta));
            ShadowShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            ShadowShader.SetFloat("shadowRes", (int)(shadowRes * shadowResolutionScale));
            ShadowShader.SetMatrix4("projection", camera.GetProjectionMatrix().Inverted() * camera.GetViewMatrix().Inverted());


            GL.Viewport(0, 0, GBuffer.Width, GBuffer.Height);
            GBuffer.Use();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            float s = MathHelper.Clamp(Vector3.Dot(Vector3.UnitY, world.sunPos), 0, 1);
            float dy = (float)Math.Pow(MathHelper.Clamp(world.sunPos.Y, 0, 1), 1 / 2.2);
            float v = (float)Math.Pow(s / 2f, 2f) * 2.2f / (1 + dy * 5) + dy + .25f;
            Vector3 sky = new Vector3((float)Math.Pow(v, 1.6), (float)Math.Pow(v, 2.2), (float)Math.Pow(v, 2.8)) / 2;
            Vector3 sunColor = new Vector3(8.6f, 8f, 7f);
            sky = Color.Saturate(sky, 2.5f);
            LightingShader.SetVector3("skyLight", sky);


            //setting uniforms 700us
            GBufferShader.SetMatrix4("view", camera.GetViewMatrix());
            GBufferShader.SetMatrix4("projection", camera.GetProjectionMatrix());
            GBufferInstancedShader.SetMatrix4("view", camera.GetViewMatrix());
            GBufferInstancedShader.SetMatrix4("projection", camera.GetProjectionMatrix());
            GBufferShader.SetVector3("viewPos", camera.Position);
            GBufferShader.SetVector3("viewDir", camera.Front);
            GBufferShader.SetVector3("rand", randVec);


            LightingShader.SetVector3("viewPos", camera.Position);
            LightingShader.SetVector3("sun.position", world.sunPos);
            LightingShader.SetVector3("sun.color", sunColor);
            LightingShader.SetMatrix4("projection", (camera.GetViewMatrix() * camera.GetProjectionMatrix()).Inverted());
            LightingShader.SetInt("lightCount", fireBalls.Length);


            TAA.SetInt("width", (int)(GBuffer.Width));
            TAA.SetInt("height", (int)(GBuffer.Height));
            TAA.SetVector3("rand", randVec);
            Final3D.SetFloat("aspect", (float)GBuffer.Width / GBuffer.Height);
            Final3D.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));


            // render camera world
            world.player.Visible = rangeCurrent > 2;
            int chunks = world.Render(GBufferShader, GBufferInstancedShader, LightingShader, camera, e.Time, frameDelta);
            ParticleSystem.Update(camera, (float)e.Time);
            ParticleSystem.Render();

            GBufferShader.SetMatrix4("model", Matrix4.CreateTranslation(-32f, 0, -32f) * Matrix4.CreateScale(64, 1, 64));
            GL.BindVertexArray(farlandVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, farlandLen);

            Matrix4 rot = Matrix4.CreateTranslation(-new Vector3(.5f)) * Matrix4.CreateRotationX((float)(TimeUtil.Unix() / 1000D % Math.PI * 2)) * Matrix4.CreateRotationY((float)(TimeUtil.Unix() / 1000D % Math.PI * 2));


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

            GL.Viewport(0, 0, VolumetricBuffer.Width, VolumetricBuffer.Height);

            VolumetricsShader.SetVector3("viewPos", camera.Position);
            //VolumetricsShader.SetMatrix4("matrixFar", matrixFar);
            VolumetricsShader.SetVector3("sun.color", sunColor);
            VolumetricsShader.SetVector3("sun.position", world.sunPos);
            VolumetricsShader.SetMatrix4("projection", (camera.GetViewMatrix() * camera.GetProjectionMatrix()).Inverted());
            VolumetricsShader.SetFloat("time", (float)(TimeUtil.Unix() / 5000d % 86400d));
            VolumetricsShader.SetFloat("shadowRes", (int)(shadowRes * shadowResolutionScale));
            VolumetricsShader.SetFloat("fogQuality", 64);

            FragmentPass.Apply(VolumetricsShader, VolumetricBuffer, GBuffer, ShadowBuffer0);


            GL.Viewport(0, 0, OcclusionBuffer0.Width, OcclusionBuffer0.Height);

            OcclusionShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            OcclusionShader.SetMatrix4("_projection", (camera.GetViewMatrix() * camera.GetProjectionMatrix()));
            OcclusionShader.SetMatrix4("projectionPrev", prevProj);
            OcclusionShader.SetMatrix4("projection", (camera.GetViewMatrix() * camera.GetProjectionMatrix()).Inverted());
            FragmentPass.Apply(OcclusionShader, pingPong ? OcclusionBuffer0 : OcclusionBuffer1, GBuffer, pingPong ? OcclusionBuffer1 : OcclusionBuffer0);

            GL.Viewport(0, 0, GBuffer.Width, GBuffer.Height);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, FireBallSSBO);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(float) * data.Length, data, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, FireBallSSBO);
            FragmentPass.Apply(LightingShader, SourceBuffer0, GBuffer, TempBuffer0, SkyBuffer, StarBuffer, VolumetricBuffer, pingPong ? OcclusionBuffer0 : OcclusionBuffer1);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            LuminanceCompute.Use();
            int[] luminanceData = new int[1];
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, LuminanceSSBO);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(int) * 1, luminanceData, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, LuminanceSSBO);
            GL.BindImageTexture(0, SourceBuffer0.colorTex[0].Handle, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba16f);
            GL.DispatchCompute(SourceBuffer0.Width / 8, SourceBuffer0.Width / 8, 1);
            //GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, sizeof(int) * 1, luminanceData);

            GL.Viewport(0, 0, SourceBuffer0.Width, SourceBuffer0.Height);

            //float maxLuma = Math.Clamp(luminanceData[0] / 512f, .4f, 5f);
            //luma = (luma * 49 + maxLuma) / 50f;

            TonemappingShader.SetFloat("maxLuma", luma);
            FragmentPass.Apply(TonemappingShader, SourceBuffer1, SourceBuffer0);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            GL.Viewport(0, 0, BloomBuffer0.Width, BloomBuffer0.Height);
            FragmentPass.Apply(BloomShader, BloomBuffer0, SourceBuffer1);

            DownsampleShader.SetFloat("tw", 1f / BloomBuffer0.Width);
            DownsampleShader.SetFloat("th", 1f / BloomBuffer0.Height);
            UpsampleShader.SetFloat("tw", 1f / BloomBuffer0.Width);
            UpsampleShader.SetFloat("th", 1f / BloomBuffer0.Height);

            DownsampleShader.SetFloat("radius", .001f * Width * resolutionScale);
            FragmentPass.Apply(DownsampleShader, BloomBuffer1, BloomBuffer0);
            DownsampleShader.SetFloat("radius", .001f * Width * resolutionScale);
            FragmentPass.Apply(DownsampleShader, BloomBuffer0, BloomBuffer1);
            DownsampleShader.SetFloat("radius", .001f * Width * resolutionScale);
            FragmentPass.Apply(DownsampleShader, BloomBuffer1, BloomBuffer0);
            DownsampleShader.SetFloat("radius", .001f * Width * resolutionScale);
            FragmentPass.Apply(DownsampleShader, BloomBuffer0, BloomBuffer1);
            UpsampleShader.SetFloat("radius", .001f * Width * resolutionScale);
            FragmentPass.Apply(UpsampleShader, BloomBuffer1, BloomBuffer0);
            UpsampleShader.SetFloat("radius", .001f * Width * resolutionScale);
            FragmentPass.Apply(UpsampleShader, BloomBuffer0, BloomBuffer1);
            UpsampleShader.SetFloat("radius", .001f * Width * resolutionScale);
            FragmentPass.Apply(UpsampleShader, BloomBuffer1, BloomBuffer0);
            UpsampleShader.SetFloat("radius", .001f * Width * resolutionScale);
            FragmentPass.Apply(UpsampleShader, BloomBuffer0, BloomBuffer1);

            GL.Viewport(0, 0, SourceBuffer1.Width, SourceBuffer1.Height);
            FragmentPass.Apply(Final3D, TempBuffer0, SourceBuffer1, BloomBuffer0);

            TAA.SetMatrix4("projection", (camera.GetViewMatrix() * camera.GetProjectionMatrix()).Inverted());
            TAA.SetMatrix4("projectionPrev", prevProj);

            FragmentPass.Apply(TAA, pingPong ? TAABuffer0 : TAABuffer1, GBuffer, TempBuffer0, pingPong ? TAABuffer1 : TAABuffer0);

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

            //FontRenderer.DrawTextWithShadow(-.95f, -.95f, .02f, "Type here to chat.");
            FontRenderer.DrawTextWithShadow(-.95f, -.95f, .02f, string.Format("{0:0.0} {1:0.0} {2:0.0}", world.player.x, world.player.y, world.player.z));
            Renderer2D.DrawRect(-.975f, -.975f, -.325f, -.97f, new(1, 1, 1, 1));

            FontRenderer.DrawTextWithShadowCentered(0, 0, .05f, message, new(1, 0, 0, 1));

            FragmentPass.BeginPostStage();

            //GL.Viewport(0, 0, TempBuffer0.Width, TempBuffer0.Height);
            //Motionblur.SetMatrix4("gbufferPreviousProjection", prevProj);
            //Motionblur.SetMatrix4("gbufferPreviousModelView", prevView);
            //prevProj = camera.GetProjectionMatrix();
            //prevView = camera.GetViewMatrix();
            //Motionblur.SetMatrix4("gbufferProjectionInverse", camera.GetProjectionMatrix().Inverted());
            //Motionblur.SetMatrix4("gbufferModelViewInverse", camera.GetViewMatrix().Inverted());
            //FragmentPass.Apply(Motionblur, TempBuffer0, SourceBuffer, GBuffer);
            
            FragmentPass.Apply(Final2D, null, pingPong ? TAABuffer0 : TAABuffer1, HUDBuffer);
            //FragmentPass.Apply(Final2D, null, ShadowBuffer0, ShadowBuffer0);

            prevProj = (camera.GetViewMatrix() * camera.GetProjectionMatrix());
            SwapBuffers();


            base.OnRenderFrame(e);
        }

        private Matrix4 RenderShadowmap(DepthAttachedFramebuffer shadowBuffer, float r, double frameDelta)
        {
            Vector3 lightPos = world.sunPos * 4096 * 1.41f;

            Vector3 cameraPos = new Vector3((int)camera.Position.X / 4 * 4, (int)camera.Position.Y / 8 * 8, (int)camera.Position.Z / 4 * 4);
            Matrix4 lsm = Matrix4.LookAt(lightPos + cameraPos, cameraPos, new Vector3(1, 0, 0)) *
                Matrix4.CreateOrthographicOffCenter(-r, r, -r, r, 1f, 8192f);
            ShadowMapShader.SetMatrix4("lightSpaceMatrix", lsm);
            ShadowMapInstancedShader.SetMatrix4("lightSpaceMatrix", lsm);

            GL.Viewport(0, 0, shadowBuffer.Width, shadowBuffer.Height);
            shadowBuffer.Use();
            GL.Clear(ClearBufferMask.DepthBufferBit);
            world.RenderWorld(camera.Position, World.renderDist / Chunk.Size + 1.4f, ShadowMapShader, ShadowMapInstancedShader, frameDelta);
            ParticleSystem.Render();
            ShadowMapShader.SetMatrix4("model", Matrix4.CreateTranslation(-32f, 0, -32f) * Matrix4.CreateScale(64, 1, 64));
            GL.BindVertexArray(farlandVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, farlandLen);
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