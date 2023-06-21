// #define RAYTRACE

using _3dTerrainGeneration.Engine.Graphics.Backend.Framebuffers;
using _3dTerrainGeneration.Engine.Graphics.Backend.Shaders;
using _3dTerrainGeneration.Engine.Graphics.Backend.Textures;
using _3dTerrainGeneration.Engine.Graphics.UI.Text;
using _3dTerrainGeneration.Engine.Physics;
using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.Graphics.UI;
using _3dTerrainGeneration.Game.World;
using _3dTerrainGeneration.Game.World.Generators;
using _3dTerrainGeneration.v2.Engine.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace _3dTerrainGeneration
{
    internal unsafe class Window : GameWindow
    {
        BiomeGenerator biomegen = new BiomeGenerator();

        public static Window Instance { get; private set; }

        public bool REGEN = true;

        private GPUProfilter profiler = new GPUProfilter();

        public FragmentShader Sky, Stars;
        private FragmentShader GBufferShader, ShadowMapShader, LightingShader, VolumetricsShader,
            ShadowShader, TAA, Final3D, Final2D, BloomShader, DownsampleShader, UpsampleShader, Motionblur,
            TonemappingShader, OcclusionShader, DOFWeightShader, DOFBlurShader, SSRShader, SharpenShader;
        private ComputeShader LuminanceCompute, LuminanceSmoothCompute;

        private DepthAttachedFramebuffer GBuffer;
        private DepthAttachedFramebuffer[] ShadowBuffers;

        private Framebuffer SourceBuffer0, SourceBuffer1, VolumetricBuffer, TempBuffer0, ShadowBuffer, BloomBuffer0, BloomBuffer1, HUDBuffer, OcclusionBuffer, TAABuffer0, TAABuffer1, DOFWeightBuffer, GIBuffer0, GIBuffer1, FinalBuffer0, FinalBuffer1;
        public Framebuffer SkyBuffer, StarBuffer;

        int FireBallSSBO, LuminanceSSBO;
        private Texture3D<byte> rtgiWorldTex;

        private Camera camera;
        private bool _firstMove = true;

        private Vector2 _lastPos;
        public World World;
        public Network network;
        private TextRenderer FontRenderer;
        public ParticleSystem ParticleSystem;
        public SoundManager SoundManager;
        public Random rnd = new Random();

        private GameSettings gameSettings = new GameSettings();

        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
            Instance = this;
        }

        static int shadowCascades = 3;

        int shadowRes = 4096;
        float resolutionScale = 1f;
        float shadowResolutionScale = 1f;
        bool pingPong = true;
        int ubuf;
        private void InitBuffers(int w, int h)
        {
            w = (int)(w * resolutionScale);
            h = (int)(h * resolutionScale);
            ubuf = GL.GenBuffer();

            if (GBuffer != null) GBuffer.Dispose();
            GBuffer = new DepthAttachedFramebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2 },
                new Texture2D(w, h, PixelInternalFormat.DepthComponent32f, PixelFormat.DepthComponent),
                new Texture2D(w, h, PixelInternalFormat.Rgba8, PixelFormat.Rgba),
                new Texture2D(w, h, (PixelInternalFormat)All.Rgb565, PixelFormat.Rgb),
                new Texture2D(w, h, PixelInternalFormat.Rgba32f, PixelFormat.Rgba).SetFilter<Texture2D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest));

            int giW = gameSettings.RTGI_Resolution;
            int giH = gameSettings.RTGI_Resolution;
            if (GIBuffer0 != null) GIBuffer0.Dispose();
            GIBuffer0 = new Framebuffer(giW, giH, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2 },
                new Texture2D(giW, giH, PixelInternalFormat.Rgba32f, PixelFormat.Rgba),
                new Texture2D(giW, giH, PixelInternalFormat.Rgba16f, PixelFormat.Rgba).SetFilter<Texture2D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest),
                new Texture2D(giW, giH, PixelInternalFormat.Rgba32f, PixelFormat.Rgba).SetFilter<Texture2D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest));

            if (GIBuffer1 != null) GIBuffer1.Dispose();
            GIBuffer1 = new Framebuffer(giW, giH, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2 },
                new Texture2D(giW, giH, PixelInternalFormat.Rgba32f, PixelFormat.Rgba),
                new Texture2D(giW, giH, PixelInternalFormat.Rgba16f, PixelFormat.Rgba).SetFilter<Texture2D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest),
                new Texture2D(giW, giH, PixelInternalFormat.Rgba32f, PixelFormat.Rgba).SetFilter<Texture2D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest));


            int sr = (int)(shadowRes * shadowResolutionScale);
            if (ShadowBuffers != null)
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
                    new Texture2D(sr, sr, PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent).SetWrap(TextureWrapMode.ClampToBorder).SetBorderColor<Texture2D>(1, 1, 1, 1));
            }

            if (SourceBuffer0 != null) SourceBuffer0.Dispose();
            SourceBuffer0 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb));

            if (SourceBuffer1 != null) SourceBuffer1.Dispose();
            SourceBuffer1 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb));

            if (VolumetricBuffer != null) VolumetricBuffer.Dispose();
            VolumetricBuffer = new Framebuffer(w / 2, h / 2, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 2, h / 2, PixelInternalFormat.Rgba16f, PixelFormat.Rgba));

            if (TempBuffer0 != null) TempBuffer0.Dispose();
            TempBuffer0 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb));

            if (ShadowBuffer != null) ShadowBuffer.Dispose();
            ShadowBuffer = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R8, PixelFormat.Red));


            if (FinalBuffer0 != null) FinalBuffer0.Dispose();
            FinalBuffer0 = new Framebuffer(Size.X, Size.Y, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(Size.X, Size.Y, PixelInternalFormat.Rgb8, PixelFormat.Rgb));
            if (FinalBuffer1 != null) FinalBuffer1.Dispose();
            FinalBuffer1 = new Framebuffer(Size.X, Size.Y, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(Size.X, Size.Y, PixelInternalFormat.Rgb8, PixelFormat.Rgb));

            if (BloomBuffer0 != null) BloomBuffer0.Dispose();
            BloomBuffer0 = new Framebuffer(w / 4, h / 4, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 4, h / 4, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb));

            if (BloomBuffer1 != null) BloomBuffer1.Dispose();
            BloomBuffer1 = new Framebuffer(w / 4, h / 4, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 4, h / 4, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb));

            if (SkyBuffer != null) SkyBuffer.Dispose();
            SkyBuffer = new Framebuffer(w / 16, h / 16, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 16, h / 16, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb));

            if (StarBuffer != null) StarBuffer.Dispose();
            StarBuffer = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.Rgb16f, PixelFormat.Rgb));

            if (HUDBuffer != null) HUDBuffer.Dispose();
            HUDBuffer = new Framebuffer(Size.X, Size.Y, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(Size.X, Size.Y, PixelInternalFormat.Rgba8, PixelFormat.Rgba));

            if (OcclusionBuffer != null) OcclusionBuffer.Dispose();
            OcclusionBuffer = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R8, PixelFormat.Red));

            if (TAABuffer0 != null) TAABuffer0.Dispose();
            TAABuffer0 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.Rgba32f, PixelFormat.Rgba).SetFilter<Texture2D>(TextureMinFilter.Linear, TextureMagFilter.Linear));
            if (TAABuffer1 != null) TAABuffer1.Dispose();
            TAABuffer1 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.Rgba32f, PixelFormat.Rgba).SetFilter<Texture2D>(TextureMinFilter.Linear, TextureMagFilter.Linear));

            if (DOFWeightBuffer != null) DOFWeightBuffer.Dispose();
            DOFWeightBuffer = new Framebuffer(w / 2, h / 2, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 2, h / 2, PixelInternalFormat.R16f, PixelFormat.Red));
        }

        int SIZE = 576;
        byte[] worldData;

        private void InitRTGIWorldTexture()
        {
            rtgiWorldTex?.Delete();
            rtgiWorldTex = new Texture3D<byte>(SIZE, SIZE, SIZE, PixelInternalFormat.R8ui, PixelFormat.RedInteger, PixelType.UnsignedByte, worldData).SetFilter<Texture3D<byte>>(TextureMinFilter.Nearest, TextureMagFilter.Nearest).SetWrap(TextureWrapMode.ClampToBorder);
        }

        private void InitShaders()
        {
#if RAYTRACE
            //REGEN = true;
            if (REGEN)
            {
                REGEN = false;

                worldData = new byte[SIZE * SIZE * SIZE];
                for (int x = 0; x < SIZE; x++)
                {
                    int _x = x - 256;
                    for (int z = 0; z < SIZE; z++)
                    {
                        int _z = z - 256;
                        float h = Chunk.GetHeight(_x, _z);

                        for (int y = 0; y < SIZE; y++)
                        {
                            int _y = y - 256;

                            if (_y < h)
                            {
                                //worldData[(z * SIZE + y) * SIZE + x] = 0x77877F6C;
                                worldData[(z * SIZE + y) * SIZE + x] = (byte)(0x01 | Color.To7Bit(135, 127, 108));
                            }
                            else if (_y - 1 < h)
                            {

                                float temp = Chunk.smoothstep(0, 1, Chunk.GetPerlin(_x, _z, .00025f) - (_y / 512f));
                                if (temp < .16)
                                {
                                    worldData[(z * SIZE + y) * SIZE + x] = (byte)(0x01 | Color.To7Bit(255, 255, 255));
                                }
                                else
                                {
                                    temp -= .16f;
                                    temp /= 1f - .16f;

                                    float humidity = Chunk.smoothstep(0, 1, Chunk.GetPerlin(_x + 12312, _z - 124124, .00025f));

                                    //if(temp > .85 && humidity < .3 && rnd.NextSingle() < .001)
                                    //{
                                    //    ImportedStructure str = new ImportedStructure("trees/cactus0/cactus0.vox", x, y + 1, z);
                                    //    str.Spawn(ref blocks, ref dataLock, X * Size, Y * Size, Z * Size);
                                    //}

                                    worldData[(z * SIZE + y) * SIZE + x] = (byte)(0x01 | Color.HsvToRgb7Bit(
                                        150 - temp * 8 * 13,
                                        166 + humidity * 4 * 16,
                                        220 - humidity * 4 * 15
                                    ));
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
            }

            InitRTGIWorldTexture();
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
            LuminanceSmoothCompute = new ComputeShader(path + "luminancesmooth.comp");
            DOFBlurShader = new FragmentShader(path + "post.vert", path + "dofblur.frag");
            DOFBlurShader.SetInt("weightTex", 0);
            DOFBlurShader.SetInt("colorTex", 1);
            DOFBlurShader.SetInt("depthTex", 2);

            DOFWeightShader = new FragmentShader(path + "post.vert", path + "dofweight.frag");
            DOFWeightShader.SetInt("depthTex", 0);

            SSRShader = new FragmentShader(path + "post.vert", path + "rtgi.frag");
            SSRShader.SetInt("data", 0);
            SSRShader.SetInt("depthTex", 1);
            SSRShader.SetInt("normalTex", 2);
            SSRShader.SetInt("SIZE", SIZE);
            SSRShader.SetInt("memoryTex", 3);
            SSRShader.SetInt("positionTex", 4);

            SharpenShader = new FragmentShader(path + "post.vert", path + "sharpen.frag");
            SharpenShader.SetInt("colorTex", 0);

            Renderer2D.LoadShader(path);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            //if (currentScreen != null)
            //    currentScreen.KeyPress((char)e.Key);
            base.OnKeyUp(e);
        }
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Keys.Backspace)
            {
                if (currentScreen != null)
                    currentScreen.BackSpacePress();
            }
            else
            {
                if (currentScreen != null)
                {
                    string key = e.Key.ToString();
                    if (key.Length == 1)
                    {
                        bool capital = (e.Modifiers & KeyModifiers.CapsLock) != 0 || (e.Modifiers & KeyModifiers.Shift) != 0;
                        currentScreen.KeyPress((capital ? key.ToUpper() : key.ToLower())[0]);
                    }
                }
            }

            base.OnKeyDown(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (currentScreen != null)
                currentScreen.MouseClicked(MouseState.X / Size.X * 2 - 1, MouseState.Y / Size.Y * -2 + 1);

            base.OnMouseDown(e);
        }

        private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            if (type == DebugType.DebugTypeOther) return;

            string messageString = Marshal.PtrToStringAnsi(message, length);
            Console.WriteLine($"{severity} {type} | {messageString}");

            //if (type == DebugType.DebugTypeError)
            //throw new Exception(messageString);
        }

        private static DebugProc _debugProcCallback = DebugCallback;
        private static GCHandle _debugProcCallbackHandle;

        protected override void OnLoad()
        {
            Console.WriteLine("OGL Version: {0}", GL.GetString(StringName.Version));
            Console.WriteLine("OGL Vendor: {0}", GL.GetString(StringName.Vendor));
            Console.WriteLine("OGL SL Version: {0}", GL.GetString(StringName.ShadingLanguageVersion));
            Console.WriteLine("OGL Renderer: {0}", GL.GetString(StringName.Renderer));
            Console.WriteLine("OGL Extensions: {0}", GL.GetString(StringName.Extensions));

#if DEBUG
            _debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);

            GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);
            //GL.Enable(EnableCap.DebugOutputSynchronous);
#endif
            Context.MakeCurrent();

            GL.Enable(EnableCap.DepthTest);
            //GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.Disable(EnableCap.Multisample);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            InitBuffers(Size.X, Size.Y);

            FragmentPass.Init();
            Renderer2D.Init();

            camera = new Camera(new Vector3(32), Size.X / (float)Size.Y);

            InitShaders();

            SoundManager = new SoundManager(gameSettings);
            FontRenderer = new TextRenderer();
            ParticleSystem = new ParticleSystem();
            currentScreen = new LoginScreen(FontRenderer, this);

            World = new World();

            FireBallSSBO = GL.GenBuffer();
            LuminanceSSBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, LuminanceSSBO);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(int) * 2, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);


            network = new Network();

            base.OnLoad();
        }

        float frameTime = 0;

        Matrix4x4 prevProj, prevView;
        bool altEnter, pr, mr, psr, msr;

        public static string message = "";
        private LoginScreen currentScreen;

        public static bool login = false;

        double frameCounter = 0;
        int frameNumber = 0;
        double dT = 1 / 20f;

        Vector2[] offsets = new Vector2[] { new(-1, 1), new(0, 1), new(1, 1),
                                            new(-1, 0), new(0, 0), new(1, 0),
                                            new(-1, -1), new(0, -1), new(1, -1)};
        int counter = 0;
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {

            if (IsFocused)
                rangeTarget -= e.OffsetY;
            base.OnMouseWheel(e);
        }

        float mouseDeltaX, mouseDeltaY;
        //Stopwatch sw = new Stopwatch();
        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            //Console.WriteLine("{0} {1} {2}", sw.ElapsedMilliseconds, e.DeltaX, e.DeltaY);
            mouseDeltaX += e.DeltaX;
            mouseDeltaY += e.DeltaY;
            base.OnMouseMove(e);
            //MouseCursor.
            //sw.Restart();
        }

        private void HandleInput(float frameDelta)
        {
            if (_firstMove)
            {
                _lastPos = new Vector2(MouseState.X, MouseState.Y);
                _firstMove = false;
            }
            else
            {
                float deltaX = (MouseState.X - _lastPos.X) * gameSettings.MouseSensitivity;
                float deltaY = -(MouseState.Y - _lastPos.Y) * gameSettings.MouseSensitivity;

                _lastPos = new Vector2(MouseState.X, MouseState.Y);

                if (IsFocused)
                {
                    camera.Yaw += deltaX;
                    camera.Pitch += deltaY;
                }

                //_camera.Yaw = (float)((world.player.GetYaw() + _camera.Yaw * 3) / 4);
                //_camera.Pitch = (float)((world.player.GetPitch() + _camera.Pitch * 3) / 4);


            }

            mouseDeltaX = 0;
            mouseDeltaY = 0;

            float pitch = (float)camera.Pitch;
            float yaw = (float)camera.Yaw;

            rangeTarget = (float)Math.Clamp(rangeTarget, 4, Math.Sqrt(gameSettings.View_Distance * 20 * .75));

            int range = 0;
            int maxRange = (int)(Math.Pow(rangeTarget, 2) / 20);

            Vector3 position = World.player.GetEyePosition(frameDelta);
            Vector3 ve = new((float)Math.Cos(OpenTK.Mathematics.MathHelper.DegreesToRadians(yaw)) * (float)Math.Cos(OpenTK.Mathematics.MathHelper.DegreesToRadians(pitch)), (float)Math.Sin(OpenTK.Mathematics.MathHelper.DegreesToRadians(pitch)), (float)Math.Sin(OpenTK.Mathematics.MathHelper.DegreesToRadians(yaw)) * (float)Math.Cos(OpenTK.Mathematics.MathHelper.DegreesToRadians(pitch)));
            if (rangeTarget >= 8)
            {

                while (range < maxRange && !(
                    World.GetBlockAt(position - ve * range) ||
                    World.GetBlockAt(position - ve * range + new Vector3(0, .5f, 0)) ||
                    World.GetBlockAt(position - ve * range + new Vector3(0, -.5f, 0)) ||
                    World.GetBlockAt(position - ve * range + new Vector3(.5f, 0, 0)) ||
                    World.GetBlockAt(position - ve * range + new Vector3(-.5f, 0, 0)) ||
                    World.GetBlockAt(position - ve * range + new Vector3(0, 0, .5f)) ||
                    World.GetBlockAt(position - ve * range + new Vector3(0, 0, -.5f))
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

#if DEBUG
            if (IsFocused && KeyboardState.IsKeyDown(Keys.R))
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
                LuminanceSmoothCompute.Dispose();
                DOFWeightShader.Dispose();
                DOFBlurShader.Dispose();
                SSRShader.Dispose();
                InitShaders();
                InitBuffers(Size.X, Size.Y);
            }
#endif

            if (IsFocused && !altEnter && KeyboardState.IsKeyDown(Keys.LeftAlt) && KeyboardState.IsKeyDown(Keys.Enter))
            {
                WindowState = WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
            }
            altEnter = KeyboardState.IsKeyDown(Keys.LeftAlt) && KeyboardState.IsKeyDown(Keys.Enter);

            if (IsFocused && !pr && KeyboardState.IsKeyDown(Keys.Equal))
            {
                resolutionScale += .125f;
                InitBuffers(Size.X, Size.Y);
            }
            pr = KeyboardState.IsKeyDown(Keys.Equal);

            if (IsFocused && !mr && KeyboardState.IsKeyDown(Keys.Minus))
            {
                resolutionScale -= .125f;
                InitBuffers(Size.X, Size.Y);
            }
            mr = KeyboardState.IsKeyDown(Keys.Minus);

            if (IsFocused && !psr && KeyboardState.IsKeyDown(Keys.RightBracket))
            {
                shadowResolutionScale += .125f;
                InitBuffers(Size.X, Size.Y);
            }
            psr = KeyboardState.IsKeyDown(Keys.RightBracket);

            if (IsFocused && !msr && KeyboardState.IsKeyDown(Keys.LeftBracket))
            {
                shadowResolutionScale -= .125f;
                InitBuffers(Size.X, Size.Y);
            }
            msr = KeyboardState.IsKeyDown(Keys.LeftBracket);
        }

        double tickBalance = 0;
        Matrix4x4[] shadowMatrices = new Matrix4x4[shadowCascades];
        float[] shadowNears = new float[] { .2f, 16f, 16f * 8 };
        float[] shadowFars = new float[] { 16f, 16f * 8, 16f * 8 * 8 };
        int[] shadowCuts = new int[shadowCascades];
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (KeyboardState.IsKeyDown(Keys.Escape) && IsFocused)
            {
                Close();
                Environment.Exit(0);
                return;
            }

            frameNumber++;
            tickBalance += e.Time;
            if (tickBalance > 0)
            {
                tickBalance -= dT;
                frameCounter = 0;

                network.Update(dT);

                if (!login)
                {
                    return;
                }

                World.player.Update(KeyboardState, camera.Yaw, camera.Pitch, MouseState.IsButtonDown(MouseButton.Left) && !LMB, MouseState.IsButtonDown(MouseButton.Right) && !RMB);
                LMB = MouseState.IsButtonDown(MouseButton.Left);
                RMB = MouseState.IsButtonDown(MouseButton.Right);

                PhysicsEngine.Current.SimulateNextFrame();
                World.Tick(dT);

                camera.Velocity = World.player.physicsData.Velocity;
            }

            profiler.BeginFrame();
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            counter++;

            pingPong = !pingPong;

            SoundManager.Update(camera, e.Time);

            float frameDelta = (float)Math.Min(frameCounter * 20, 1);
            frameCounter += e.Time;
            FontRenderer.SetAspectRatio(Size.X / (float)Size.Y);

            if (network == null)
            {
                GL.Viewport(0, 0, Size.X, Size.Y);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit);

                FontRenderer.DrawTextWithShadowCentered(0, 0, .05f, "Connecting to the server...");

                SwapBuffers();

                base.OnRenderFrame(e);
                return;
            }

            if (!login)
            {
                GL.Viewport(0, 0, Size.X, Size.Y);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit);

                if (currentScreen != null)
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


            Vector2 taaJitter = offsets[counter % 9] * new Vector2(1f / GBuffer.Width, 1f / GBuffer.Height) * .5f;

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            //FireBall[] fireBalls = world.entities[TerrainServer.network.EntityType.FireBall].Values.Cast<FireBall>().ToArray();

            //LightingShader.SetInt("lightCount", fireBalls.Length + 1);
            //float[] data = new float[fireBalls.Length * 4 + 4];
            //for (int i = 1; i < fireBalls.Length + 1; i++)
            //{
            //    Vector3 vec3 = fireBalls[i - 1].GetPositionInterpolated(frameDelta);
            //    data[i * 4] = vec3.X;
            //    data[i * 4 + 1] = vec3.Y;
            //    data[i * 4 + 2] = vec3.Z;
            //    data[i * 4 + 3] = fireBalls[i - 1].Radius;
            //}

            Vector3 _pos = World.player.GetPositionInterpolated(frameDelta);

            //data[0] = _pos.X + (float)Math.Cos((world.player.yaw + 90) / 180 * Math.PI) * .5f;
            //data[1] = _pos.Y + 1.1f;
            //data[2] = _pos.Z + (float)Math.Sin((world.player.yaw + 90) / 180 * Math.PI) * .5f;
            //data[3] = 0.01f;

            int chunksShadow = 0;
            // render shadowmap
            World.player.Visible = true;
            SSRShader.SetVector2("taaOffset", taaJitter);
            SSRShader.SetVector2("wh", new Vector2(GIBuffer0.Width, GIBuffer0.Height));
            ShadowMapShader.SetVector2("taaOffset", taaJitter);
            ShadowShader.SetVector2("taaOffset", taaJitter);
            ShadowMapShader.SetMatrix4("view", camera.GetViewMatrix());
            ShadowMapShader.SetMatrix4("projection", camera.GetProjectionMatrix());

            for (int i = 0; i < shadowCascades; i++)
            {
                profiler.Start("Shadowmap" + i);
                shadowMatrices[i] = RenderShadowmap(ShadowBuffers[i], shadowNears[i], shadowFars[i], frameDelta);
                profiler.End();
            }

            //int shInd = frameNumber % 2 + 1;
            //profiler.Start("Shadowmap");
            //shadowMatrices[0] = RenderShadowmap(ShadowBuffers[0], shadowNears[0], shadowFars[0], frameDelta);
            //shadowMatrices[shInd] = RenderShadowmap(ShadowBuffers[shInd], shadowNears[shInd], shadowFars[shInd], frameDelta);
            //profiler.End();

            ShadowShader.SetMatrix4Arr("matrices[0]", shadowMatrices);
            ShadowShader.SetFloatArr("cuts[0]", shadowFars);
            VolumetricsShader.SetMatrix4Arr("matrices[0]", shadowMatrices);
            VolumetricsShader.SetFloatArr("cuts[0]", shadowFars);
            VolumetricsShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            ShadowShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            ShadowShader.SetMatrix4("projection", viewProjInv);


            GL.Viewport(0, 0, GBuffer.Width, GBuffer.Height);
            GBuffer.Use();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Vector3 sunColor = new Vector3(9f, 6.3f, 5.5f);

            Vector3 c = new(.055f, .130f, .224f);

            float smoothstep(float edge0, float edge1, float x)
            {
                if (x < edge0)
                    return 0;

                if (x >= edge1)
                    return 1;

                // Scale/bias into [0..1] range
                x = (x - edge0) / (edge1 - edge0);

                return x * x * (3 - 2 * x);
            }
            float stuff = MathF.Pow(smoothstep(0f, 1f, World.SunPosition.Y / 2 + .5f), 24f) * 80f;


            Vector3 col = new(MathF.Pow(stuff, c.X), MathF.Pow(stuff, c.Y), MathF.Pow(stuff, c.Z));
            Vector3 sky = col / 10f + c / 20f;

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
            LightingShader.SetVector3("sun.position", World.SunPosition);
            LightingShader.SetVector3("sun.color", sunColor);
            LightingShader.SetMatrix4("projection", viewProjInv);
            LightingShader.SetInt("giW", GIBuffer0.Width);
            LightingShader.SetInt("giH", GIBuffer0.Height);
            LightingShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 3600d));

            Final3D.SetFloat("aspect", (float)GBuffer.Width / GBuffer.Height);
            Final3D.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));

            // render camera world
            World.player.Visible = rangeCurrent > 2;
            profiler.Start("World");
            int chunks = World.Render(camera, e.Time, frameDelta);

            SceneRenderer.Instance.Render(GBufferShader);

            profiler.End();
            ParticleSystem.Update(camera, (float)e.Time);
            profiler.Start("Particles");
            ParticleSystem.Render();
            profiler.End();

            GL.Disable(EnableCap.Blend);
            FragmentPass.PreparePass();


            Texture[] depthColorShadowTex = new Texture[shadowCascades + 2];
            depthColorShadowTex[0] = GBuffer.depthTex0;
            depthColorShadowTex[1] = GBuffer.colorTex[1];
            for (int i = 0; i < shadowCascades; i++)
            {
                depthColorShadowTex[i + 2] = ShadowBuffers[i].depthTex0;
            }
            profiler.Start("ShadowFilter");
            FragmentPass.ApplyPass(ShadowShader, ShadowBuffer, depthColorShadowTex);
            profiler.End();

            GL.Viewport(0, 0, SkyBuffer.Width, SkyBuffer.Height);
            Sky.SetMatrix4("projection", projInvMatrix);
            Sky.SetMatrix4("viewMatrix", viewMatrix);
            Sky.SetVector3("viewPos", camera.Position);
            Sky.SetVector3("sun_dir", World.SunPosition);
            Sky.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 3600d));
            profiler.Start("Sky");
            FragmentPass.ApplyPass(Sky, SkyBuffer);
            profiler.End();

            GL.Viewport(0, 0, StarBuffer.Width, StarBuffer.Height);
            Stars.SetMatrix4("projection", projInvMatrix);
            Stars.SetMatrix4("viewMatrix", viewMatrix);
            Stars.SetVector3("sun_dir", World.SunPosition);
            Stars.SetFloat("time", (float)(TimeUtil.Unix() / 100000d % 3600d));
            profiler.Start("Stars");
            FragmentPass.ApplyPass(Stars, StarBuffer);
            profiler.End();

            GL.Viewport(0, 0, VolumetricBuffer.Width, VolumetricBuffer.Height);

            VolumetricsShader.SetVector3("viewPos", camera.Position);
            VolumetricsShader.SetVector3("sun.color", sunColor);
            VolumetricsShader.SetVector3("sun.position", World.SunPosition);
            VolumetricsShader.SetMatrix4("projection", viewProjInv);
            VolumetricsShader.SetFloat("time", (float)(TimeUtil.Unix() / 5000d % 86400d));

            profiler.Start("Volumetrics");
            FragmentPass.ApplyPass(VolumetricsShader, VolumetricBuffer, depthColorShadowTex);
            profiler.End();


            GL.Viewport(0, 0, OcclusionBuffer.Width, OcclusionBuffer.Height);

            OcclusionShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            OcclusionShader.SetMatrix4("_projection", viewProj);
            OcclusionShader.SetMatrix4("projectionPrev", prevProj);
            OcclusionShader.SetMatrix4("projection", viewProjInv);
            profiler.Start("SSAO");
            FragmentPass.ApplyPass(OcclusionShader, OcclusionBuffer, GBuffer.depthTex0, GBuffer.colorTex[1]);
            profiler.End();

#if RAYTRACE

            GL.Viewport(0, 0, GIBuffer0.Width, GIBuffer0.Height);

            SSRShader.SetMatrix4("projection", viewProjInv);
            SSRShader.SetMatrix4("_projection", viewProj);
            SSRShader.SetMatrix4("projectionPrev", prevProj);
            SSRShader.SetVector3("position", camera.Position);
            SSRShader.SetVector3("viewDir", camera.Front);
            SSRShader.SetVector3("sunDir", World.sunPos);
            SSRShader.SetFloat("time", (float)(TimeUtil.Unix() / 5000d % 1d));

            profiler.Start("RayTrace");
            FragmentPass.Apply(SSRShader, pingPong ? GIBuffer0 : GIBuffer1,
                rtgiWorldTex,
                GBuffer.depthTex0,
                GBuffer.colorTex[1],
                pingPong ? GIBuffer1.colorTex[0] : GIBuffer0.colorTex[0],
                GBuffer.colorTex[2]
            );
            profiler.End();

#endif

            GL.Viewport(0, 0, GBuffer.Width, GBuffer.Height);

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, FireBallSSBO);
            //GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(float) * data.Length, data, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, FireBallSSBO);
            profiler.Start("Shading");
#if RAYTRACE
            FragmentPass.Apply(LightingShader, SourceBuffer1,
                GBuffer.depthTex0, GBuffer.colorTex[0], GBuffer.colorTex[1],
                ShadowBuffer.colorTex[0],
                SkyBuffer.colorTex[0],
                StarBuffer.colorTex[0],
                VolumetricBuffer.colorTex[0],
                OcclusionBuffer.colorTex[0],
                (pingPong ? GIBuffer0 : GIBuffer1).colorTex[1],
                (pingPong ? GIBuffer0 : GIBuffer1).colorTex[2]
            );
#else
            FragmentPass.ApplyPass(LightingShader, SourceBuffer1,
                GBuffer.depthTex0, GBuffer.colorTex[0], GBuffer.colorTex[1],
                ShadowBuffer.colorTex[0],
                SkyBuffer.colorTex[0],
                StarBuffer.colorTex[0],
                VolumetricBuffer.colorTex[0],
                OcclusionBuffer.colorTex[0]
            );
#endif
            profiler.End();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            LuminanceCompute.Use();

            profiler.Start("Luminance");
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, LuminanceSSBO);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, LuminanceSSBO);
            GL.BindImageTexture(0, SourceBuffer1.colorTex[0].Handle, 0, false, 0, TextureAccess.ReadOnly, (SizedInternalFormat)35898);
            GL.DispatchCompute(SourceBuffer1.Width / 16 / 8, SourceBuffer1.Height / 16 / 8, 1);
            profiler.End();
            LuminanceSmoothCompute.Use();
            profiler.Start("Luminance Smooth");
            GL.DispatchCompute(1, 1, 1);
            profiler.End();


            GL.Viewport(0, 0, SourceBuffer1.Width, SourceBuffer1.Height);

            byte[] getBytes(Matrix4x4 str)
            {
                byte[] arr = new byte[64];

                int idx = 0;
                Array.Copy(BitConverter.GetBytes(str.M11), 0, arr, idx, 4); idx += 4;
                Array.Copy(BitConverter.GetBytes(str.M21), 0, arr, idx, 4); idx += 4;
                Array.Copy(BitConverter.GetBytes(str.M31), 0, arr, idx, 4); idx += 4;
                Array.Copy(BitConverter.GetBytes(str.M41), 0, arr, idx, 4); idx += 4;

                Array.Copy(BitConverter.GetBytes(str.M12), 0, arr, idx, 4); idx += 4;
                Array.Copy(BitConverter.GetBytes(str.M22), 0, arr, idx, 4); idx += 4;
                Array.Copy(BitConverter.GetBytes(str.M32), 0, arr, idx, 4); idx += 4;
                Array.Copy(BitConverter.GetBytes(str.M42), 0, arr, idx, 4); idx += 4;

                Array.Copy(BitConverter.GetBytes(str.M13), 0, arr, idx, 4); idx += 4;
                Array.Copy(BitConverter.GetBytes(str.M23), 0, arr, idx, 4); idx += 4;
                Array.Copy(BitConverter.GetBytes(str.M33), 0, arr, idx, 4); idx += 4;
                Array.Copy(BitConverter.GetBytes(str.M43), 0, arr, idx, 4); idx += 4;

                Array.Copy(BitConverter.GetBytes(str.M14), 0, arr, idx, 4); idx += 4;
                Array.Copy(BitConverter.GetBytes(str.M24), 0, arr, idx, 4); idx += 4;
                Array.Copy(BitConverter.GetBytes(str.M34), 0, arr, idx, 4); idx += 4;
                Array.Copy(BitConverter.GetBytes(str.M44), 0, arr, idx, 4); idx += 4;

                return arr;
            }

            byte[] getBytesV(Vector2 str)
            {
                int size = Marshal.SizeOf(str);
                byte[] arr = new byte[size];

                IntPtr ptr = IntPtr.Zero;
                try
                {
                    ptr = Marshal.AllocHGlobal(size);
                    Marshal.StructureToPtr(str, ptr, true);
                    Marshal.Copy(ptr, arr, 0, size);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
                return arr;
            }

            byte[] udata = new byte[176];
            Array.Copy(BitConverter.GetBytes(GBuffer.Width), 0, udata, 0, 4);
            Array.Copy(BitConverter.GetBytes(GBuffer.Height), 0, udata, 4, 4);
            Array.Copy(getBytes(prevProj), 0, udata, 16, 64);
            Array.Copy(getBytes(viewProjInv), 0, udata, 80, 64);
            Array.Copy(getBytesV(taaJitter), 0, udata, 144, 8);

            GL.BindBuffer(BufferTarget.UniformBuffer, ubuf);

            GL.BufferData(BufferTarget.UniformBuffer, udata.Length, udata, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 3, ubuf);
            //TAA.SetInt("width", (int)(GBuffer.Width));
            //TAA.SetInt("height", (int)(GBuffer.Height));
            //TAA.SetMatrix4("projectionPrev", prevProj);
            //TAA.SetMatrix4("projection", viewProjInv);
            //TAA.SetVector2("taaOffset", taaJitter);

            profiler.Start("TAA");
            FragmentPass.ApplyPass(TAA, pingPong ? TAABuffer0 : TAABuffer1, GBuffer.depthTex0, SourceBuffer1.colorTex[0], (pingPong ? TAABuffer1 : TAABuffer0).colorTex[0]);
            profiler.End();

            SharpenShader.SetFloat("width", TempBuffer0.Width);
            SharpenShader.SetFloat("height", TempBuffer0.Height);
            profiler.Start("Sharpen");
            FragmentPass.ApplyPass(SharpenShader, TempBuffer0, (pingPong ? TAABuffer0 : TAABuffer1).colorTex[0]);
            profiler.End();

            DOFBlurShader.SetFloat("aspectRatio", Size.X / (float)Size.Y);
            GL.Viewport(0, 0, DOFWeightBuffer.Width, DOFWeightBuffer.Height);
            profiler.Start("Dof Weight");
            FragmentPass.ApplyPass(DOFWeightShader, DOFWeightBuffer, GBuffer.depthTex0);
            profiler.End();
            GL.Viewport(0, 0, SourceBuffer1.Width, SourceBuffer1.Height);
            profiler.Start("Dof Blur");
            FragmentPass.ApplyPass(DOFBlurShader, SourceBuffer1, DOFWeightBuffer.colorTex[0], TempBuffer0.colorTex[0], GBuffer.depthTex0);
            //FragmentPass.Apply(DOFBlurShader, SourceBuffer0, DOFWeightBuffer.colorTex[0], SourceBuffer1.colorTex[0]);
            //FragmentPass.Apply(DOFBlurShader, SourceBuffer1, DOFWeightBuffer.colorTex[0], SourceBuffer0.colorTex[0]);
            profiler.End();

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, LuminanceSSBO);
            //GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, sizeof(int) * 1, luminanceData);
            profiler.Start("Apply Tonemap");
            TonemappingShader.SetFloat("width", SourceBuffer0.Width);
            TonemappingShader.SetFloat("height", SourceBuffer0.Height);
            TonemappingShader.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            FragmentPass.ApplyPass(TonemappingShader, SourceBuffer0, SourceBuffer1.colorTex[0]);
            profiler.End();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            GL.Viewport(0, 0, BloomBuffer0.Width, BloomBuffer0.Height);
            profiler.Start("Bloom");
            FragmentPass.ApplyPass(BloomShader, BloomBuffer0, SourceBuffer0.colorTex[0]);
            profiler.End();

            DownsampleShader.SetFloat("tw", 1f / BloomBuffer0.Width);
            DownsampleShader.SetFloat("th", 1f / BloomBuffer0.Height);
            UpsampleShader.SetFloat("tw", 1f / BloomBuffer0.Width);
            UpsampleShader.SetFloat("th", 1f / BloomBuffer0.Height);

            profiler.Start("Bloom Blur");
            float blRad = .001f * Size.X * resolutionScale;
            DownsampleShader.SetFloat("radius", blRad);
            for (int i = 0; i < 10; i++)
            {
                FragmentPass.ApplyPass(DownsampleShader, BloomBuffer1, BloomBuffer0.colorTex[0]);
                FragmentPass.ApplyPass(DownsampleShader, BloomBuffer0, BloomBuffer1.colorTex[0]);
            }

            UpsampleShader.SetFloat("radius", blRad);
            for (int i = 0; i < 10; i++)
            {
                FragmentPass.ApplyPass(UpsampleShader, BloomBuffer1, BloomBuffer0.colorTex[0]);
                FragmentPass.ApplyPass(UpsampleShader, BloomBuffer0, BloomBuffer1.colorTex[0]);
            }

            profiler.End();

            GL.Viewport(0, 0, Size.X, Size.Y);
            profiler.Start("Final3D");
            FragmentPass.ApplyPass(Final3D, FinalBuffer1, SourceBuffer0.colorTex[0], BloomBuffer0.colorTex[0]);
            profiler.End();
            GL.Viewport(0, 0, Size.X, Size.Y);

            HUDBuffer.Use();
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

            frameTime = (float)(e.Time + frameTime * 29) / 30;
            FontRenderer.DrawTextWithShadowCentered(0, 1f - .06f * Size.X / Size.Y, .025f, string.Format("FPS: {0:00} | GEO VRAM: {1} / {2} MB | TEX VRAM: {3} MB", (int)(1 / frameTime), SceneRenderer.Instance.VramUsage / 1024 / 1024, SceneRenderer.Instance.VramAllocated / 1024 / 1024, Texture.TotalBytesAllocated / 1024 / 1024));

            FontRenderer.DrawTextWithShadow(-1 + .025f, 1f - .075f * FontRenderer.aspectRatio, .025f, string.Format("Day {0} {1:00}:{2:00}", (int)(World.Time / 1000 / 1440), (int)(World.Time / 1000 / 1440 * 24 % 24), (int)(World.Time / 1000 / 1440 * 24 * 60 % 60)));
            FontRenderer.DrawTextWithShadow(-1 + .025f, 1f - .125f * FontRenderer.aspectRatio, .025f, "Lands of Pososich");

            Renderer2D.DrawRect(-.3f, -.825f, -.0125f, -.775f, new(186 / 255f, 43 / 255f, 43 / 255f, 1));
            FontRenderer.DrawTextWithShadowCentered((-.3f - .0125f) / 2f, -.8f, .0125f, string.Format("{0:0.0} / {1} HP", World.player.health, World.player.maxHealth));

            Renderer2D.DrawRect(.0125f, -.825f, .3f, -.775f, new(52 / 255f, 147 / 255f, 235 / 255f, 1));
            FontRenderer.DrawTextWithShadowCentered((.3f + .0125f) / 2f, -.8f, .0125f, "1000 / 1000 MP");

            //if(pingPong)
            //{
            //    Renderer2D.DrawRect(-1, -1, -.5f, -.5f, new(1, 1, 1, 1));
            //}

            Renderer2D.DrawRect(-.3f, -.85f, .3f, -1, new(0, 0, 0, .5f));

            //FontRenderer.DrawTextWithShadow(-.95f, -.95f, .02f, "Type here to chat.");
            BiomeInfo info = biomegen.GetBiomeInfo((int)World.player.GetPosition().X, (int)World.player.GetPosition().Z);
            FontRenderer.DrawTextWithShadow(-.95f, -.9f, .02f, string.Format("{0:0.0}deg h{1:0.0}% f{2:0.0}%", info.Temperature, info.Humidity, info.Fertility));

            FontRenderer.DrawTextWithShadow(-.95f, -.95f, .02f, string.Format("{0:0.0} {1:0.0} {2:0.0}", World.player.GetPosition().X, World.player.GetPosition().Y, World.player.GetPosition().Z));
            Renderer2D.DrawRect(-.975f, -.975f, -.325f, -.97f, new(1, 1, 1, 1));

            FontRenderer.DrawTextWithShadowCentered(0, 0, .05f, message, new(1, 0, 0, 1));

            FontRenderer.DrawTextWithShadow(-1, .5f, .0125f, "Frame summary:");
            List<string> times = profiler.GetTimes();
            for (int i = 0; i < times.Count; i++)
            {
                FontRenderer.DrawTextWithShadow(-1, .5f - (i + 1) * .025f, .0125f, times[i]);
            }

            GL.Disable(EnableCap.Blend);
            FragmentPass.PreparePass();

            //GL.Viewport(0, 0, TempBuffer0.Width, TempBuffer0.Height);
            //Motionblur.SetMatrix4("projection", viewProjInv);
            //Motionblur.SetMatrix4("projectionPrev", prevProj);
            //Motionblur.SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));
            //profiler.Start("MotionBlur");
            //FragmentPass.Apply(Motionblur, FinalBuffer0, FinalBuffer1.colorTex[0], GBuffer.depthTex0);
            //profiler.End();

            GL.Viewport(0, 0, Size.X, Size.Y);
            profiler.Start("Final2D");
            //FragmentPass.Apply(Final2D, null, FinalBuffer0.colorTex[0], HUDBuffer.colorTex[0]);
            FragmentPass.ApplyPass(Final2D, null, FinalBuffer1.colorTex[0], HUDBuffer.colorTex[0]);
            profiler.End();

            prevProj = viewProj;
            prevView = viewMatrix;
            profiler.EndFrame();
            SwapBuffers();
            CursorState = CursorState.Grabbed;

            OpenTK.Graphics.OpenGL.ErrorCode err = GL.GetError();
            if (err != OpenTK.Graphics.OpenGL.ErrorCode.NoError)
            {
                // Console.WriteLine("OGL Error: {0}!", err);
            }

            base.OnRenderFrame(e);
        }

        private List<Vector4> GetFrustumCornersWorldSpace(Matrix4x4 proj, Matrix4x4 view)
        {
            Matrix4x4 inv = view * proj;
            Matrix4x4.Invert(view * proj, out inv);

            List<Vector4> frustumCorners = new List<Vector4>();
            for (int x = 0; x < 2; ++x)
            {
                for (int y = 0; y < 2; ++y)
                {
                    for (int z = 0; z < 2; ++z)
                    {
                        Vector4 pt = Vector4.Transform(new Vector4(2.0f * x - 1.0f, 2.0f * y - 1.0f, 2.0f * z - 1.0f, 1.0f), inv);
                        frustumCorners.Add(pt / pt.W);
                    }
                }
            }

            return frustumCorners;
        }

        private Matrix4x4 RenderShadowmap(DepthAttachedFramebuffer shadowBuffer, float n, float f, float frameDelta)
        {
            GL.Disable(EnableCap.CullFace);
            List<Vector4> corners = GetFrustumCornersWorldSpace(Matrix4x4.CreatePerspectiveFieldOfView(OpenTK.Mathematics.MathHelper.DegreesToRadians(camera.Fov), camera.AspectRatio, n, f), camera.GetViewMatrix());
            Vector4 center = new Vector4();
            foreach (var c in corners)
            {
                center += c;
            }

            center /= corners.Count;
            Vector3 cn = new Vector3(center.X, center.Y, center.Z);
            Matrix4x4 look = Matrix4x4.CreateLookAt(cn + World.SunPosition, cn, new Vector3(0, 1, 0));

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var c in corners)
            {
                Matrix4x4 linv = look;
                Vector4 v = Vector4.Transform(c, linv);

                minX = Math.Min(v.X / v.W, minX);
                minY = Math.Min(v.Y / v.W, minY);
                minZ = Math.Min(v.Z / v.W, minZ);

                maxX = Math.Max(v.X / v.W, maxX);
                maxY = Math.Max(v.Y / v.W, maxY);
                maxZ = Math.Max(v.Z / v.W, maxZ);
            }

            Matrix4x4 lightProjection = Matrix4x4.CreateOrthographicOffCenter(minX, maxX, minY, maxY, 0, 8000);
            Matrix4x4 lsm = look * lightProjection;

            ShadowMapShader.SetMatrix4("lightSpaceMatrix", lsm);

            GL.Viewport(0, 0, shadowBuffer.Width, shadowBuffer.Height);
            shadowBuffer.Use();
            GL.Clear(ClearBufferMask.DepthBufferBit);
            World.RenderWorld(camera.Position, look * lightProjection, true, frameDelta);
            SceneRenderer.Instance.Render(ShadowMapShader);

            GL.Enable(EnableCap.CullFace);
            ParticleSystem.Render();
            return lsm;
        }

        bool LMB, RMB;
        float rangeCurrent = 0;
        float rangeTarget = 8;
        float wheel = 0;

        protected override void OnUpdateFrame(FrameEventArgs e)
        {


            base.OnUpdateFrame(e);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            camera.AspectRatio = e.Size.X / (float)e.Size.Y;
            InitBuffers(e.Size.X, e.Size.Y);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
        }
    }

}