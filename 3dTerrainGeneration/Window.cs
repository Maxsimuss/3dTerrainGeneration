﻿//#define RASTER
#define RAYTRACE


using System;
using System.Collections.Generic;
using System.Numerics;
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
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using LibNoise.Model;
using System.Data.SqlTypes;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace _3dTerrainGeneration
{
    internal unsafe class Window : OpenTK.GameWindow
    {
        public static Window Instance { get; private set; }

        private GPUProfilter profiler = new GPUProfilter();

        public FragmentShader Sky, Stars;
        private FragmentShader GBufferShader, ShadowMapShader, LightingShader, VolumetricsShader, 
            ShadowShader, TAA, Final3D, Final2D, BloomShader, DownsampleShader, UpsampleShader, Motionblur, 
            TonemappingShader, OcclusionShader, DOFWeightShader, DOFBlurShader, SSRShader, SharpenShader;
        private ComputeShader LuminanceCompute;

        private DepthAttachedFramebuffer GBuffer;
        private DepthAttachedFramebuffer[] ShadowBuffers;

        private Framebuffer SourceBuffer0, SourceBuffer1, VolumetricBuffer, TempBuffer0, BloomBuffer0, BloomBuffer1, HUDBuffer, 
            OcclusionBuffer, TAABuffer0, TAABuffer1, DOFWeightBuffer, GIBuffer0, GIBuffer1;
        public Framebuffer SkyBuffer, StarBuffer;

        int FireBallSSBO, LuminanceSSBO, worldTexHandle;
        Texture noiseTexture = null;

        private Camera camera;
        private bool _firstMove = true;

        private Vector2 _lastPos;
        private World world;
        public Network network;
        private FontRenderer FontRenderer;
        public ParticleSystem ParticleSystem;
        public SoundManager SoundManager;

        public Window(int w, int h, OpenTK.GameWindowFlags flag, string title) : base(w, h, new GraphicsMode(new ColorFormat(8, 8, 8, 0)), title, flag, OpenTK.DisplayDevice.Default, 4, 20, GraphicsContextFlags.ForwardCompatible) {
            Location = new System.Drawing.Point(OpenTK.DisplayDevice.Default.Width / 2 - w / 2, OpenTK.DisplayDevice.Default.Height / 2 - h / 2);
            Instance = this;
        }

        int shadowCascades = 3;

        int shadowRes = 4096;
        float resolutionScale = 1f;
        float shadowResolutionScale = 1f;
        bool pingPong = true;

        private void InitBuffers(int w, int h)
        {
            w = (int)(w * resolutionScale);
            h = (int)(h * resolutionScale);

            if(GBuffer != null) GBuffer.Dispose();
            GBuffer = new DepthAttachedFramebuffer(w, h, new[] {DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1},
                new Texture(w, h, PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent),
                new Texture(w, h, PixelInternalFormat.Rgba8, PixelFormat.Rgba),
                new Texture(w, h, PixelInternalFormat.Rgba8, PixelFormat.Rgba));

            int giR = 16;

            if (GIBuffer0 != null) GIBuffer0.Dispose();
            GIBuffer0 = new Framebuffer(w / giR, h / giR, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2 },
                new Texture(w / giR, h / giR, PixelInternalFormat.Rgba16f, PixelFormat.Rgba, filtered: true),
                new Texture(w / giR, h / giR, PixelInternalFormat.Rgba16f, PixelFormat.Rgba, filtered: false),
                new Texture(w / giR, h / giR, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, filtered: false));

            if (GIBuffer1 != null) GIBuffer1.Dispose();
            GIBuffer1 = new Framebuffer(w / giR, h / giR, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2 },
                new Texture(w / giR, h / giR, PixelInternalFormat.Rgba16f, PixelFormat.Rgba, filtered: true),
                new Texture(w / giR, h / giR, PixelInternalFormat.Rgba16f, PixelFormat.Rgba, filtered: false),
                new Texture(w / giR, h / giR, PixelInternalFormat.Rgba32f, PixelFormat.Rgba, filtered: false));


            int sr = (int)(shadowRes * shadowResolutionScale);
            if(ShadowBuffers != null)
            {
                foreach (var item in ShadowBuffers)
                {
                    item.Dispose();
                }
            }
            ShadowBuffers = new DepthAttachedFramebuffer[shadowCascades];
            for (int i = 0; i < shadowCascades; i++)
            {
                ShadowBuffers[i] = new DepthAttachedFramebuffer(sr, sr, new DrawBuffersEnum[0],
                    new Texture(sr, sr, PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent, filtered: true, border: true, mode: TextureCompareMode.None));
            }

            if (SourceBuffer0 != null) SourceBuffer0.Dispose();
            SourceBuffer0 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(w, h, PixelInternalFormat.Rgba16f));

            if (SourceBuffer1 != null) SourceBuffer1.Dispose();
            SourceBuffer1 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(w, h, PixelInternalFormat.Rgba16f));

            if (VolumetricBuffer != null) VolumetricBuffer.Dispose();
            VolumetricBuffer = new Framebuffer(w / 2, h / 2, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(w / 2, h / 2, PixelInternalFormat.R8, PixelFormat.Red));

            if (TempBuffer0 != null) TempBuffer0.Dispose();
            TempBuffer0 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(w, h, PixelInternalFormat.Rgba16f));

            if (BloomBuffer0 != null) BloomBuffer0.Dispose();
            BloomBuffer0 = new Framebuffer(w / 4, h / 4, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(w / 4, h / 4, PixelInternalFormat.Rgb16f));

            if (BloomBuffer1 != null) BloomBuffer1.Dispose();
            BloomBuffer1 = new Framebuffer(w / 4, h / 4, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(w / 4, h / 4, PixelInternalFormat.Rgb16f));

            if (SkyBuffer != null) SkyBuffer.Dispose();
            SkyBuffer = new Framebuffer(w / 16, h / 16, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(w / 16, h / 16, PixelInternalFormat.Rgb16f));

            if (StarBuffer != null) StarBuffer.Dispose();
            StarBuffer = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(w, h, PixelInternalFormat.Rgb));

            if (HUDBuffer != null) HUDBuffer.Dispose();
            HUDBuffer = new Framebuffer(Width, Height, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(Width, Height, PixelInternalFormat.Rgba, PixelFormat.Rgba));

            if (OcclusionBuffer != null) OcclusionBuffer.Dispose();
            OcclusionBuffer = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(w, h, PixelInternalFormat.R8, PixelFormat.Red));

            if (TAABuffer0 != null) TAABuffer0.Dispose();
            TAABuffer0 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(w, h, PixelInternalFormat.Rgba16f, filtered: true));
            if (TAABuffer1 != null) TAABuffer1.Dispose();
            TAABuffer1 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(w, h, PixelInternalFormat.Rgba16f, filtered: true));

            if (DOFWeightBuffer != null) DOFWeightBuffer.Dispose();
            DOFWeightBuffer = new Framebuffer(w / 2, h / 2, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture(w / 2, h / 2, PixelInternalFormat.R16f, PixelFormat.Red));

            if (noiseTexture != null) noiseTexture.Dispose();
            noiseTexture = Texture.FromFile(@"C:\Users\maksi\Downloads\BlueNoise470.png");
            noiseTexture.Repeat();
        }

        int SIZE = 512;
        uint[] worldData;

        private void InitShaders()
        {
#if !RASTER
            SIZE = 512;


            GL.DeleteTexture(worldTexHandle);

            worldData = new uint[SIZE * SIZE * SIZE];
            for (int x = 0; x < SIZE; x++)
            {
                int _x = x - 256;
                for (int z = 0; z < SIZE; z++)
                {
                    int _z = z - 256;
                    for (int y = 0; y < SIZE; y++)
                    {
                        int _y = y - 256;
                        if (Chunk.GetNoise(_x, _y, _z, .05f) > .75f)
                        {
                            if(Chunk.GetNoise(_x, _y + 2, _z, .05f) > .75f)
                            {
                                worldData[(z * SIZE + y) * SIZE + x] = 0x77000000 | Color.ToInt(128, 128, 128);
                            }
                            else
                            {
                                worldData[(z * SIZE + y) * SIZE + x] = 0x77000000 | Color.ToInt((byte)_x, (byte)_y, (byte)_z);
                            }
                        }
                    }
                }
            }
            //for (int x = 0; x < SIZE; x++)
            //{
            //    for (int z = 0; z < SIZE; z++)
            //    {
            //        int X = x * 2 + -200;
            //        int Z = z * 2 + 3100;

            //        float h = (float)Math.Pow(Chunk.OcataveNoise(X, Z, .0005f / 4, 8) * 1.2, 7) * Chunk.GetPerlin(X, Z, .0005f / 4) * 255 * 2;

            //        float humidity = Chunk.smoothstep(0, 1, Chunk.GetPerlin(X + 12312, Z - 124124, .00025f));

            //        for (int y = 0; y < SIZE; y++)
            //        {
            //            if (y < h)
            //            {
            //                worldData[(x * SIZE + y) * SIZE + z] = 0x77877F6C;
            //            }
            //            else if (y - 1 < h)
            //            {
            //                float temp = Chunk.smoothstep(0, 1, Chunk.GetPerlin(x, z, .00025f) - (y * 2 / 2512f));

            //                if (temp < .16)
            //                {
            //                    worldData[(x * SIZE + y) * SIZE + z] = Color.ToInt(255, 255, 255) | 0x77000000;
            //                }
            //                else
            //                {
            //                    temp -= .16f;
            //                    temp /= 1f - .16f;


            //                    //if(temp > .85 && humidity < .3 && rnd.NextSingle() < .001)
            //                    //{
            //                    //    ImportedStructure str = new ImportedStructure("trees/cactus0/cactus0.vox", x, y + 1, z);
            //                    //    str.Spawn(ref blocks, ref dataLock, X * Size, Y * Size, Z * Size);
            //                    //}

            //                    worldData[(x * SIZE + y) * SIZE + z] = Color.HsvToRgb(
            //                            150 - temp * 8 * 13,
            //                            166 + humidity * 4 * 16,
            //                            220 - humidity * 4 * 15
            //                    ) | 0x77000000;
            //                }
            //            }
            //        }
            //    }
            //}

            worldTexHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture3D, worldTexHandle);
            GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.Rgba, SIZE, SIZE, SIZE, 0, PixelFormat.Rgba, PixelType.UnsignedByte, worldData);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToBorder);
            
#endif

#if DEBUG
            string path = "../../../shaders/";
#else
            string path = "shaders/";
#endif
            GBufferShader = new FragmentShader(path + "gbuffer.vert", path + "gbuffer.frag");
            ShadowMapShader = new FragmentShader(path + "shadowmap.vert", path + "empty.frag");

            ShadowShader = new FragmentShader(path + "post.vert", path + "shadow.frag");
            ShadowShader.SetInt("depthTex", 0);
            ShadowShader.SetInt("normalTex", 1);
            int[] shadowTexes = new int[shadowCascades];
            for (int i = 0; i < shadowCascades; i++)
            {
                shadowTexes[i] = i + 2;
            }
            ShadowShader.SetIntArr("shadowTex[0]", shadowTexes);

            LightingShader = new FragmentShader(path + "post.vert", path + "lighting.frag");
            LightingShader.SetInt("depthTex", 0);
            LightingShader.SetInt("colorTex", 1);
            LightingShader.SetInt("normalTex", 2);
            LightingShader.SetInt("shadowTex", 3);
            LightingShader.SetInt("skyTex", 4);
            LightingShader.SetInt("starTex", 5);
            LightingShader.SetInt("fogTex", 6);
            LightingShader.SetInt("occlusionTex", 7);
            LightingShader.SetInt("giTex", 8);
            LightingShader.SetInt("giNTex", 9);

            OcclusionShader = new FragmentShader(path + "post.vert", path + "occlusion.frag");
            OcclusionShader.SetInt("depthTex", 0);
            OcclusionShader.SetInt("normalTex", 1);

            VolumetricsShader = new FragmentShader(path + "post.vert", path + "volumetrics.frag");
            VolumetricsShader.SetInt("depthTex", 0);
            VolumetricsShader.SetInt("normalTex", 1);
            VolumetricsShader.SetIntArr("shadowTex[0]", shadowTexes);

            TAA = new FragmentShader(path + "post.vert", path + "post/taa.frag");
            TAA.SetInt("depthTex", 0);
            TAA.SetInt("colorTex0", 1);
            TAA.SetInt("colorTex1", 2);

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
            DOFBlurShader = new FragmentShader(path + "post.vert", path + "dofblur.frag");
            DOFBlurShader.SetInt("weightTex", 0);
            DOFBlurShader.SetInt("colorTex", 1);

            DOFWeightShader = new FragmentShader(path + "post.vert", path + "dofweight.frag");
            DOFWeightShader.SetInt("depthTex", 0);

            SSRShader = new FragmentShader(path + "post.vert", path + "ssr.frag");
            SSRShader.SetInt("data", 0);
            SSRShader.SetInt("depthTex", 1);
            SSRShader.SetInt("normalTex", 2);
            SSRShader.SetInt("SIZE", SIZE);
            SSRShader.SetInt("memoryTex", 3);

            SharpenShader = new FragmentShader(path + "post.vert", path + "sharpen.frag");
            SharpenShader.SetInt("colorTex", 0);

            Renderer2D.LoadShader(path);
        }

        protected override void OnKeyPress(OpenTK.KeyPressEventArgs e)
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
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.Multisample);

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

            world = new World();

            FireBallSSBO = GL.GenBuffer();
            LuminanceSSBO = GL.GenBuffer();

            Task.Run(() =>
            {
                network = new Network(world);
                world.network = network;
            });

            base.OnLoad(e);
        }

        float frameTime = 0;

        Matrix4x4 prevProj, prevView;
        bool altEnter, pr, mr, psr, msr;

        public static string message = "";
        private LoginScreen currentScreen;

        public static bool login = false;

        double frameCounter = 0;

        double dT = 1 / 20f;

        float luma = 2;

        Vector2[] offsets = new Vector2[] { new(-1, 1), new(0, 1), new(1, 1),
                                            new(-1, 0), new(0, 0), new(1, 0),
                                            new(-1, -1), new(0, -1), new(1, -1)};
        int counter = 0;
        private void HandleInput(double frameDelta)
        {
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
            Vector3 ve = new((float)Math.Cos(OpenTK.MathHelper.DegreesToRadians(yaw)) * (float)Math.Cos(OpenTK.MathHelper.DegreesToRadians(pitch)), (float)Math.Sin(OpenTK.MathHelper.DegreesToRadians(pitch)), (float)Math.Sin(OpenTK.MathHelper.DegreesToRadians(yaw)) * (float)Math.Cos(OpenTK.MathHelper.DegreesToRadians(pitch)));
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
                ShadowMapShader.Dispose();
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
                DOFWeightShader.Dispose();
                DOFBlurShader.Dispose();
                SSRShader.Dispose();
                InitShaders();
                //InitBuffers(Width, Height);
            }
#endif

            if (Focused && !altEnter && Keyboard.GetState().IsKeyDown(Key.AltLeft) && Keyboard.GetState().IsKeyDown(Key.Enter))
            {
                WindowState = (WindowState == OpenTK.WindowState.Fullscreen) ? OpenTK.WindowState.Normal : OpenTK.WindowState.Fullscreen;
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
        }

        protected override void OnRenderFrame(OpenTK.FrameEventArgs e)
        {
            profiler.BeginFrame();
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
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

                if(currentScreen != null)
                    currentScreen.Render();

                SwapBuffers();

                base.OnRenderFrame(e);
                return;
            }
            currentScreen = null;

            HandleInput(frameDelta);

            Matrix4x4 viewMatrix = camera.GetViewMatrix();
            Matrix4x4 viewInvMatrix;
            Matrix4x4.Invert(viewMatrix, out viewInvMatrix);
            //Matrix4x4.Invert(viewMatrix, out viewMatrix);
            Matrix4x4 projMatrix = camera.GetProjectionMatrix();
            //Matrix4x4.Invert(projMatrix, out projMatrix);
            Matrix4x4 projInvMatrix;
            Matrix4x4.Invert(projMatrix, out projInvMatrix);

            Matrix4x4 viewProj = viewMatrix * projMatrix;
            Matrix4x4 viewProjInv;
            Matrix4x4.Invert(viewProj, out viewProjInv);


            Vector2 taaJitter = offsets[counter % 9] * new Vector2(1f / GBuffer.Width, 1f / GBuffer.Height) * 0;

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            FireBall[] fireBalls = world.entities[TerrainServer.network.EntityType.FireBall].Values.Cast<FireBall>().ToArray();
            float[] data = new float[fireBalls.Length * 4 + 4];
            for (int i = 1; i < fireBalls.Length + 1; i++)
            {
                Vector3 vec3 = fireBalls[i - 1].GetPositionInterpolated(frameDelta);
                data[i * 4] = vec3.X;
                data[i * 4 + 1] = vec3.Y;
                data[i * 4 + 2] = vec3.Z;
                data[i * 4 + 3] = fireBalls[i - 1].Radius;
            }

            Vector3 _pos = world.player.GetPositionInterpolated(frameDelta);

            data[0] = _pos.X + (float)Math.Cos((world.player.yaw + 90) / 180 * Math.PI) * .5f;
            data[1] = _pos.Y + 1.1f;
            data[2] = _pos.Z + (float)Math.Sin((world.player.yaw + 90) / 180 * Math.PI) * .5f;
            data[3] = 0.01f;


            int chunksShadow = 0;
            // render shadowmap
            world.player.Visible = true;
            SSRShader.SetVector2("taaOffset", taaJitter);
            ShadowMapShader.SetVector2("taaOffset", taaJitter);
            ShadowShader.SetVector2("taaOffset", taaJitter);
            ShadowMapShader.SetMatrix4("view", camera.GetViewMatrix());
            ShadowMapShader.SetMatrix4("projection", camera.GetProjectionMatrix());
            Matrix4x4[] matrices = new Matrix4x4[shadowCascades];
            float near = .2f;
            float far = 16;
            int[] cuts = new int[shadowCascades];
            for (int i = 0; i < shadowCascades; i++)
            {
                cuts[i] = (int)far;
                profiler.Start("Shadowmap" + i);
                matrices[i] = RenderShadowmap(ShadowBuffers[i], near, far, frameDelta);
                profiler.End();
                near = far;
                far = far * 8;
            }
            ShadowShader.SetMatrix4Arr("matrices[0]", matrices);
            ShadowShader.SetIntArr("cuts[0]", cuts);
            VolumetricsShader.SetMatrix4Arr("matrices[0]", matrices);
            VolumetricsShader.SetIntArr("cuts[0]", cuts);
            VolumetricsShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            ShadowShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            ShadowShader.SetMatrix4("projection", viewProjInv);


            GL.Viewport(0, 0, GBuffer.Width, GBuffer.Height);
            GBuffer.Use();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            float s = OpenTK.MathHelper.Clamp(Vector3.Dot(Vector3.UnitY, World.sunPos), 0, 1);
            float dy = (float)Math.Pow(OpenTK.MathHelper.Clamp(World.sunPos.Y, 0, 1), 1 / 2.2);
            float v = (float)Math.Pow(s / 2f, 2f) * 2.2f / (1 + dy * 5) + dy + .25f;
            Vector3 sky = new Vector3((float)Math.Pow(v, 1.6), (float)Math.Pow(v, 2.2), (float)Math.Pow(v, 2.8)) / 2;
            Vector3 sunColor = new Vector3(8.6f, 8f, 7f);
            sky = Color.Saturate(sky, 2.5f);
            LightingShader.SetVector3("skyLight", sky);
            SSRShader.SetVector3("skyLight", sky);
            SSRShader.SetVector3("sunLight", sunColor);


            //setting uniforms 700us
            GBufferShader.SetMatrix4("view", camera.GetViewMatrix());
            GBufferShader.SetMatrix4("projection", camera.GetProjectionMatrix());
            GBufferShader.SetVector3("viewPos", camera.Position);
            GBufferShader.SetVector3("viewDir", camera.Front);
            GBufferShader.SetVector2("taaOffset", taaJitter);

            LightingShader.SetVector3("viewPos", camera.Position);
            LightingShader.SetVector3("sun.position", World.sunPos);
            LightingShader.SetVector3("sun.color", sunColor);
            LightingShader.SetMatrix4("projection", viewProjInv);
            LightingShader.SetInt("giW", GIBuffer0.Width);
            LightingShader.SetInt("giH", GIBuffer0.Height);


            TAA.SetInt("width", (int)(GBuffer.Width));
            TAA.SetInt("height", (int)(GBuffer.Height));
            TAA.SetVector2("taaOffset", taaJitter);
            Final3D.SetFloat("aspect", (float)GBuffer.Width / GBuffer.Height);
            Final3D.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));

            // render camera world
            world.player.Visible = rangeCurrent > 2;
            profiler.Start("World");
            int chunks = world.Render(GBufferShader, LightingShader, camera, e.Time, frameDelta);
            profiler.End();
            ParticleSystem.Update(camera, (float)e.Time);
            profiler.Start("Particles");
            ParticleSystem.Render();
            profiler.End();

            GL.Disable(EnableCap.Blend);
            FragmentPass.BeginPostStage();


            Texture[] textures = new Texture[shadowCascades + 2];
            textures[0] = GBuffer.depthTex0;
            textures[1] = GBuffer.colorTex[1];
            for (int i = 0; i < shadowCascades; i++)
            {
                textures[i + 2] = ShadowBuffers[i].depthTex0;
            }
            profiler.Start("ShadowFilter");
            FragmentPass.Apply(ShadowShader, TempBuffer0, textures);
            profiler.End();

            GL.Viewport(0, 0, SkyBuffer.Width, SkyBuffer.Height);
            Sky.SetMatrix4("projection", projInvMatrix);
            Sky.SetMatrix4("viewMatrix", viewMatrix);
            Sky.SetVector3("viewPos", camera.Position);
            Sky.SetVector3("sun_dir", World.sunPos);
            Sky.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 3600d));
            profiler.Start("Sky");
            FragmentPass.Apply(Sky, SkyBuffer);
            profiler.End();

            GL.Viewport(0, 0, StarBuffer.Width, StarBuffer.Height);
            Stars.SetMatrix4("projection", projInvMatrix);
            Stars.SetMatrix4("viewMatrix", viewMatrix);
            Stars.SetVector3("sun_dir", World.sunPos);
            Stars.SetFloat("time", (float)(TimeUtil.Unix() / 100000d % 3600d));
            profiler.Start("Stars");
            FragmentPass.Apply(Stars, StarBuffer);
            profiler.End();

            GL.Viewport(0, 0, VolumetricBuffer.Width, VolumetricBuffer.Height);

            VolumetricsShader.SetVector3("viewPos", camera.Position);
            VolumetricsShader.SetVector3("sun.color", sunColor);
            VolumetricsShader.SetVector3("sun.position", World.sunPos);
            VolumetricsShader.SetMatrix4("projection", viewProjInv);
            VolumetricsShader.SetFloat("time", (float)(TimeUtil.Unix() / 5000d % 86400d));

            profiler.Start("Volumetrics");
            FragmentPass.Apply(VolumetricsShader, VolumetricBuffer, GBuffer.depthTex0, GBuffer.colorTex[1]/*, ShadowBuffer0.depthTex0*/);
            profiler.End();


            GL.Viewport(0, 0, OcclusionBuffer.Width, OcclusionBuffer.Height);

            OcclusionShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            OcclusionShader.SetMatrix4("_projection", viewProj);
            OcclusionShader.SetMatrix4("projectionPrev", prevProj);
            OcclusionShader.SetMatrix4("projection", viewProjInv);
            profiler.Start("SSAO");
            FragmentPass.Apply(OcclusionShader, OcclusionBuffer, GBuffer.depthTex0, GBuffer.colorTex[1], noiseTexture);
            profiler.End();

            GL.Viewport(0, 0, GIBuffer0.Width, GIBuffer0.Height);

            SSRShader.SetMatrix4("projection", viewProjInv);
            SSRShader.SetMatrix4("_projection", viewProj);
            SSRShader.SetMatrix4("projectionPrev", prevProj);
            SSRShader.SetVector3("position", camera.Position);
            SSRShader.SetVector3("viewDir", camera.Front);
            SSRShader.SetVector3("sunDir", World.sunPos);
            SSRShader.SetFloat("time", (float)(TimeUtil.Unix() / 5000d % 1d));

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture3D, worldTexHandle);

            profiler.Start("RayTrace");
            FragmentPass.Apply(SSRShader, pingPong ? GIBuffer0 : GIBuffer1, 1, 
                GBuffer.depthTex0, 
                GBuffer.colorTex[1], 
                pingPong ? GIBuffer1.colorTex[0] : GIBuffer0.colorTex[0]
            );
            profiler.End();

            GL.Viewport(0, 0, GBuffer.Width, GBuffer.Height);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, FireBallSSBO);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(float) * data.Length, data, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, FireBallSSBO);
            profiler.Start("Shading");
            FragmentPass.Apply(LightingShader, SourceBuffer1, 
                GBuffer.depthTex0, GBuffer.colorTex[0], GBuffer.colorTex[1],
                TempBuffer0.colorTex[0],
                SkyBuffer.colorTex[0], 
                StarBuffer.colorTex[0], 
                VolumetricBuffer.colorTex[0], 
                OcclusionBuffer.colorTex[0],
                (pingPong ? GIBuffer0 : GIBuffer1).colorTex[1],
                (pingPong ? GIBuffer0 : GIBuffer1).colorTex[2]
            );
            profiler.End();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            LuminanceCompute.Use();
            int[] luminanceData = new int[1];
            profiler.Start("Luminance");
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, LuminanceSSBO);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(int) * 1, luminanceData, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, LuminanceSSBO);
            GL.BindImageTexture(0, SourceBuffer1.colorTex[0].Handle, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.Rgba16f);
            GL.DispatchCompute(SourceBuffer1.Width / 32, SourceBuffer1.Height / 32, 1);

            profiler.End();

            GL.Viewport(0, 0, SourceBuffer1.Width, SourceBuffer1.Height);

            TAA.SetMatrix4("projection", viewProjInv);
            TAA.SetMatrix4("projectionPrev", prevProj);

            profiler.Start("TAA");
            FragmentPass.Apply(TAA, pingPong ? TAABuffer0 : TAABuffer1, GBuffer.depthTex0, SourceBuffer1.colorTex[0], (pingPong ? TAABuffer1 : TAABuffer0).colorTex[0]);
            profiler.End();

            SharpenShader.SetFloat("width", TempBuffer0.Width);
            SharpenShader.SetFloat("height", TempBuffer0.Height);
            profiler.Start("Sharpen");
            FragmentPass.Apply(SharpenShader, TempBuffer0, (pingPong ? TAABuffer0 : TAABuffer1).colorTex[0]);
            profiler.End();

            DOFBlurShader.SetFloat("aspectRatio", Width / (float)Height);
            GL.Viewport(0, 0, DOFWeightBuffer.Width, DOFWeightBuffer.Height);
            profiler.Start("Dof Weight");
            FragmentPass.Apply(DOFWeightShader, DOFWeightBuffer, GBuffer.depthTex0);
            profiler.End();
            GL.Viewport(0, 0, SourceBuffer1.Width, SourceBuffer1.Height);
            profiler.Start("Dof Blur");
            FragmentPass.Apply(DOFBlurShader, SourceBuffer1, DOFWeightBuffer.colorTex[0], TempBuffer0.colorTex[0]);
            //FragmentPass.Apply(DOFBlurShader, SourceBuffer0, DOFWeightBuffer.colorTex[0], SourceBuffer1.colorTex[0]);
            //FragmentPass.Apply(DOFBlurShader, SourceBuffer1, DOFWeightBuffer.colorTex[0], SourceBuffer0.colorTex[0]);
            profiler.End();

            GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, sizeof(int) * 1, luminanceData);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            float maxLuma = Math.Clamp(luminanceData[0] / 1024f, .4f, 8f);
            luma = (luma * 49 + maxLuma) / 50f;
            //luma = 4;

            profiler.Start("Apply Tonemap");
            TonemappingShader.SetFloat("maxLuma", luma);
            TonemappingShader.SetFloat("width", SourceBuffer0.Width);
            TonemappingShader.SetFloat("height", SourceBuffer0.Height);
            TonemappingShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            FragmentPass.Apply(TonemappingShader, SourceBuffer0, SourceBuffer1.colorTex[0]);
            profiler.End();

            GL.Viewport(0, 0, BloomBuffer0.Width, BloomBuffer0.Height);
            profiler.Start("Bloom");
            FragmentPass.Apply(BloomShader, BloomBuffer0, SourceBuffer0.colorTex[0]);
            profiler.End();

            DownsampleShader.SetFloat("tw", 1f / BloomBuffer0.Width);
            DownsampleShader.SetFloat("th", 1f / BloomBuffer0.Height);
            UpsampleShader.SetFloat("tw", 1f / BloomBuffer0.Width);
            UpsampleShader.SetFloat("th", 1f / BloomBuffer0.Height);

            profiler.Start("Bloom Blur");
            DownsampleShader.SetFloat("radius", .0005f * Width * resolutionScale);
            FragmentPass.Apply(DownsampleShader, BloomBuffer1, BloomBuffer0.colorTex[0]);
            DownsampleShader.SetFloat("radius", .0005f * Width * resolutionScale);
            FragmentPass.Apply(DownsampleShader, BloomBuffer0, BloomBuffer1.colorTex[0]);
            DownsampleShader.SetFloat("radius", .0005f * Width * resolutionScale);
            FragmentPass.Apply(DownsampleShader, BloomBuffer1, BloomBuffer0.colorTex[0]);
            DownsampleShader.SetFloat("radius", .0005f * Width * resolutionScale);
            FragmentPass.Apply(DownsampleShader, BloomBuffer0, BloomBuffer1.colorTex[0]);
            UpsampleShader.SetFloat("radius", .0005f * Width * resolutionScale);
            FragmentPass.Apply(UpsampleShader, BloomBuffer1, BloomBuffer0.colorTex[0]);
            UpsampleShader.SetFloat("radius", .0005f * Width * resolutionScale);
            FragmentPass.Apply(UpsampleShader, BloomBuffer0, BloomBuffer1.colorTex[0]);
            UpsampleShader.SetFloat("radius", .0005f * Width * resolutionScale);
            FragmentPass.Apply(UpsampleShader, BloomBuffer1, BloomBuffer0.colorTex[0]);
            UpsampleShader.SetFloat("radius", .0005f * Width * resolutionScale);
            FragmentPass.Apply(UpsampleShader, BloomBuffer0, BloomBuffer1.colorTex[0]);
            profiler.End();

            GL.Viewport(0, 0, TempBuffer0.Width, TempBuffer0.Height);
            profiler.Start("Final3D");
            FragmentPass.Apply(Final3D, TempBuffer0, SourceBuffer0.colorTex[0], BloomBuffer0.colorTex[0]);
            profiler.End();
            GL.Viewport(0, 0, Width, Height);

            HUDBuffer.Use();
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

            frameTime = (float)(e.Time + frameTime * 29) / 30;
            FontRenderer.DrawTextWithShadowCentered(0, 1f - .06f * Width / Height, .025f, string.Format("FPS: {0:00} | VRAM: {1} / {2} MB", (int)(1 / frameTime), World.gameRenderer.VramUsage / 1024 / 1024, World.gameRenderer.VramAllocated / 1024 / 1024));

            FontRenderer.DrawTextWithShadow(-1 + .025f, 1f - .075f * FontRenderer.aspectRatio, .025f, string.Format("Day {0} {1:00}:{2:00}", (int)(world.Time / 1000 / 1440), (int)(world.Time / 1000 / 1440 * 24 % 24), (int)(world.Time / 1000 / 1440 * 24 * 60 % 60)));
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

            FontRenderer.DrawTextWithShadow(-1, .5f, .0125f, "Frame summary:");
            List<string> times = profiler.GetTimes();
            for (int i = 0; i < times.Count; i++)
            {
                FontRenderer.DrawTextWithShadow(-1, .5f - (i + 1) * .025f, .0125f, times[i]);
            }

            GL.Disable(EnableCap.Blend);
            FragmentPass.BeginPostStage();

            GL.Viewport(0, 0, TempBuffer0.Width, TempBuffer0.Height);
            Motionblur.SetMatrix4("projection", viewProjInv);
            Motionblur.SetMatrix4("projectionPrev", prevProj);
            FragmentPass.Apply(Motionblur, SourceBuffer0, TempBuffer0.colorTex[0], GBuffer.depthTex0);

            GL.Viewport(0, 0, Width, Height);
            profiler.Start("Final2D");
            FragmentPass.Apply(Final2D, null, SourceBuffer0.colorTex[0], HUDBuffer.colorTex[0]);
            profiler.End();
            //FragmentPass.Apply(Final2D, null, ShadowBuffer0, ShadowBuffer0);

            prevProj = viewProj;
            prevView = viewMatrix;
            profiler.EndFrame();
            SwapBuffers();

            base.OnRenderFrame(e);
        }

        private List<Vector4> GetFrustumCornersWorldSpace(Matrix4x4 proj, Matrix4x4 view)
        {
            Matrix4x4 inv = view * proj;
            Matrix4x4.Invert(view * proj, out inv);

            List<Vector4> frustumCorners = new List<Vector4>();
            for (int x = 0; x< 2; ++x)
            {
                for (int y = 0; y< 2; ++y)
                {
                    for (int z = 0; z< 2; ++z)
                    {
                        Vector4 pt = Vector4.Transform(new Vector4(2.0f * x - 1.0f, 2.0f * y - 1.0f, 2.0f * z - 1.0f, 1.0f), inv);
                        frustumCorners.Add(pt / pt.W);
                    }
                }
            }
    
            return frustumCorners;
        }

        private Matrix4x4 RenderShadowmap(DepthAttachedFramebuffer shadowBuffer, float n, float f, double frameDelta)
        {
            GL.Disable(EnableCap.CullFace);
            List<Vector4> corners = GetFrustumCornersWorldSpace(Matrix4x4.CreatePerspectiveFieldOfView(OpenTK.MathHelper.DegreesToRadians(camera.Fov), camera.AspectRatio, n, f), camera.GetViewMatrix());
            Vector4 center = new Vector4();
            foreach (var c in corners)
            {
                center += c;
            }

            center /= corners.Count;
            Vector3 cn = new Vector3(center.X, center.Y, center.Z);
            Matrix4x4 look = Matrix4x4.CreateLookAt(cn + World.sunPos, cn, new Vector3(0, 1, 0));

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var c in corners)
            {
                Matrix4x4 linv = look;
                //Matrix4x4.Invert(look, out linv);
                Vector4 v = Vector4.Transform(c, linv);
                
                minX = Math.Min(v.X / v.W, minX);
                minY = Math.Min(v.Y / v.W, minY);
                minZ = Math.Min(v.Z / v.W, minZ);

                maxX = Math.Max(v.X / v.W, maxX);
                maxY = Math.Max(v.Y / v.W, maxY);
                maxZ = Math.Max(v.Z / v.W, maxZ);
            }


            // Tune this parameter according to the scene
            float zMult = 10.0f;
            if (minZ < 0)
            {
                minZ *= zMult;
            }
            else
            {
                minZ /= zMult;
            }
            if (maxZ < 0)
            {
                maxZ /= zMult;
            }
            else
            {
                maxZ *= zMult;
            }

            Matrix4x4 lightProjection = Matrix4x4.CreateOrthographicOffCenter(minX, maxX, minY, maxY, 0, 8000);
            Matrix4x4 lsm = look * lightProjection;

            ShadowMapShader.SetMatrix4("lightSpaceMatrix", lsm);

            GL.Viewport(0, 0, shadowBuffer.Width, shadowBuffer.Height);
            shadowBuffer.Use();
            GL.Clear(ClearBufferMask.DepthBufferBit);
            world.RenderWorld(camera.Position, look * lightProjection, ShadowMapShader, false, true, frameDelta);
            GL.Enable(EnableCap.CullFace);
            ParticleSystem.Render();
            return lsm;
        }

        bool LMB, RMB;
        float rangeCurrent = 0;
        float rangeTarget = 8;
        float wheel = 0;

        protected override void OnUpdateFrame(OpenTK.FrameEventArgs e)
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