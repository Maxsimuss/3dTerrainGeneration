using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.Graphics._3D.Cameras;
using _3dTerrainGeneration.Engine.Graphics.Backend;
using _3dTerrainGeneration.Engine.Graphics.Backend.Framebuffers;
using _3dTerrainGeneration.Engine.Graphics.Backend.RenderActions;
using _3dTerrainGeneration.Engine.Graphics.Backend.Shaders;
using _3dTerrainGeneration.Engine.Graphics.Backend.Textures;
using _3dTerrainGeneration.Engine.Graphics.UI;
using _3dTerrainGeneration.Engine.Options;
using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Game.GameWorld.Generators;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace _3dTerrainGeneration.Engine.Graphics
{
    internal class GraphicsEngine
    {
        private static GraphicsEngine instance;
        public static GraphicsEngine Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GraphicsEngine();
                }

                return instance;
            }
        }

        private static readonly int SHADOW_CASCADES = 3;

        private ComputeShader LuminanceCompute, LuminanceSmoothCompute;

        private DepthAttachedFramebuffer GBuffer;
        private DepthAttachedFramebuffer[] ShadowBuffers;

        private Framebuffer RGB11BitBuffer0, RGB11BitBuffer1, VolumetricBuffer, ShadowBuffer, BloomBuffer0, BloomBuffer1, HUDBuffer, OcclusionBuffer, TAABuffer0, TAABuffer1, DOFWeightBuffer, GIBuffer0, GIBuffer1, RGB8BtBuffer;
        private Framebuffer SkyBuffer, StarBuffer;

        private List<IRenderAction> renderActions = new List<IRenderAction>();

        private Matrix4x4[] shadowMatrices = new Matrix4x4[SHADOW_CASCADES];
        private float[] shadowNears = new float[] { .2f, 16f, 16f * 8 };
        private float[] shadowFars = new float[] { 16f, 16f * 8, 16f * 8 * 8 };

        private ISceneLayer mainLayer => Game.MainLayer;
        private ISceneLayer shadowLayer => Game.ShadowLayer;

        public Camera Camera { get; private set; } = new Camera(Vector3.Zero, 1);
        public ICameraPositionProvider CameraPositionProvider = new DemoCameraPositionProvider();
        public Vector3 SunPosition => Game.World.SunPosition + new Vector3(.001f);

        private int _width = 640, _height = 480;
        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                AspectRatio = (float)Width / Height;
            }
        }

        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                AspectRatio = (float)Width / Height;
            }
        }

        public float AspectRatio { get => Camera.AspectRatio; set => Camera.AspectRatio = value; }

        public float TickFraction = 0;

        public Vector3 Lerp(Vector3 from, Vector3 to)
        {
            return from * (1 - TickFraction) + to * TickFraction;
        }

        public float Lerp(float from, float to)
        {
            return from * (1 - TickFraction) + to * TickFraction;
        }

        public int FrameIndex = 0;
        public double FrameTimeMillis = 0;
        public double FrameTimeAvg = 0;

        public IGame Game;

        private static DebugProc _debugProcCallback = DebugCallback;
        private static GCHandle _debugProcCallbackHandle;

        private Matrix4x4 viewMatrix, viewInvMatrix, projMatrix, projInvMatrix, viewProjMatrix, viewProjInvMatrix;
        private Matrix4x4 prevViewMatrix, prevProjMatrix, prevViewProjMatrix;
        private Vector2 taaJitter;

        private GraphicsEngine()
        {
            Console.WriteLine("OGL Version: {0}", GL.GetString(StringName.Version));
            Console.WriteLine("OGL Vendor: {0}", GL.GetString(StringName.Vendor));
            Console.WriteLine("OGL SL Version: {0}", GL.GetString(StringName.ShadingLanguageVersion));
            Console.WriteLine("OGL Renderer: {0}", GL.GetString(StringName.Renderer));
            Console.WriteLine("OGL Extensions: {0}", GL.GetString(StringName.Extensions));

            _debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);

            GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);

            OptionManager.Instance.RegisterOption("Graphics", "3D Resolution Scale", .125, 2, 1, .25);
            OptionManager.Instance.RegisterOption("Graphics", "Sharpness", 0, 1, .25, .1);
            OptionManager.Instance.RegisterOption("Graphics", "Shadows Enabled", true);
            OptionManager.Instance.RegisterOption("Graphics", "Shadow Resolution", 512, 8192, 4096, 1024);
            OptionManager.Instance.RegisterOption("Graphics", "SSAO Enabled", true);
            OptionManager.Instance.RegisterOption("Graphics", "SSAO Quality", 1, 16, 4);
            OptionManager.Instance.RegisterOption("Graphics", "RTGI Enabled", false);
            OptionManager.Instance.RegisterOption("Graphics", "RTGI Resolution", 1, 16, 1);
            OptionManager.Instance.RegisterOption("Graphics", "RTGI Quality", 1, 16, 1);

            OptionManager.Instance.OnOptionsChanged += OnOptionsChanged;

            Reload();
        }

        private void OnOptionsChanged(string category, string name)
        {
            if (category != "Graphics")
            {
                return;
            }

            switch (name)
            {
                case "Sharpness":
                case "SSAO Quality":
                    InitShaders();
                    SetupPipeline();
                    break;

                default:
                    Reload();
                    break;
            }
        }

        private static void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
        {
            if (type == DebugType.DebugTypeOther) return;

            string messageString = Marshal.PtrToStringAnsi(message, length);
            Console.WriteLine($"{severity} {type} | {messageString}");

            //if (type == DebugType.DebugTypeError)
            //throw new Exception(messageString);
        }

        public void Reload()
        {
            InitBuffers(Width, Height);
            InitShaders();
            SetupPipeline();
        }

        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;
            AspectRatio = (float)Width / Height;
            Reload();
        }

        private void InitBuffers(int w, int h)
        {
            float resolutionScale = (float)OptionManager.Instance["Graphics", "3D Resolution Scale"];
            w = (int)(w * resolutionScale);
            h = (int)(h * resolutionScale);

            if (GBuffer != null) GBuffer.Dispose();
            GBuffer = new DepthAttachedFramebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 },
                new Texture2D(w, h, PixelInternalFormat.DepthComponent32f),
                new Texture2D(w, h, PixelInternalFormat.Rgba8),
                new Texture2D(w, h, (PixelInternalFormat)All.Rgb565));

            if (OptionManager.Instance["Graphics", "RTGI Enabled"])
            {
                int giW = w / (int)OptionManager.Instance["Graphics", "RTGI Resolution"];
                int giH = h / (int)OptionManager.Instance["Graphics", "RTGI Resolution"];
                if (GIBuffer0 != null) GIBuffer0.Dispose();
                GIBuffer0 = new Framebuffer(giW, giH, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2 },
                    new Texture2D(giW, giH, PixelInternalFormat.Rgba32f),
                    new Texture2D(giW, giH, PixelInternalFormat.Rgba16f).SetFilter<Texture2D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest),
                    new Texture2D(giW, giH, PixelInternalFormat.Rgba32f).SetFilter<Texture2D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest));

                if (GIBuffer1 != null) GIBuffer1.Dispose();
                GIBuffer1 = new Framebuffer(giW, giH, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2 },
                    new Texture2D(giW, giH, PixelInternalFormat.Rgba32f),
                    new Texture2D(giW, giH, PixelInternalFormat.Rgba16f).SetFilter<Texture2D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest),
                    new Texture2D(giW, giH, PixelInternalFormat.Rgba32f).SetFilter<Texture2D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest));

                //if (rtgiData != null) rtgiData.Delete();
                //rtgiData = new Texture3D(512, 512, 512, PixelInternalFormat.R8ui);
            }
            else
            {
                //if (rtgiData != null)
                //{
                //    rtgiData.Delete();
                //    rtgiData = null;
                //}
            }

            int shadowResoluion = (int)OptionManager.Instance["Graphics", "Shadow Resolution"];
            if (ShadowBuffers != null)
            {
                foreach (var item in ShadowBuffers)
                {
                    item.Dispose();
                }
            }
            ShadowBuffers = new DepthAttachedFramebuffer[SHADOW_CASCADES];
            for (int i = 0; i < SHADOW_CASCADES; i++)
            {
                ShadowBuffers[i] = new DepthAttachedFramebuffer(shadowResoluion, shadowResoluion, new DrawBuffersEnum[0],
                    new Texture2D(shadowResoluion, shadowResoluion, PixelInternalFormat.DepthComponent24).SetWrap(TextureWrapMode.ClampToBorder).SetBorderColor<Texture2D>(1, 1, 1, 1));
            }

            if (RGB11BitBuffer0 != null) RGB11BitBuffer0.Dispose();
            RGB11BitBuffer0 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f));

            if (RGB11BitBuffer1 != null) RGB11BitBuffer1.Dispose();
            RGB11BitBuffer1 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f));

            if (VolumetricBuffer != null) VolumetricBuffer.Dispose();
            VolumetricBuffer = new Framebuffer(w / 2, h / 2, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 2, h / 2, PixelInternalFormat.Rgba16f));

            if (ShadowBuffer != null) ShadowBuffer.Dispose();
            ShadowBuffer = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R8));

            if (RGB8BtBuffer != null) RGB8BtBuffer.Dispose();
            RGB8BtBuffer = new Framebuffer(Width, Height, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(Width, Height, PixelInternalFormat.Rgb8));

            if (BloomBuffer0 != null) BloomBuffer0.Dispose();
            BloomBuffer0 = new Framebuffer(w / 4, h / 4, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 4, h / 4, PixelInternalFormat.R11fG11fB10f));

            if (BloomBuffer1 != null) BloomBuffer1.Dispose();
            BloomBuffer1 = new Framebuffer(w / 4, h / 4, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 4, h / 4, PixelInternalFormat.R11fG11fB10f));

            if (SkyBuffer != null) SkyBuffer.Dispose();
            SkyBuffer = new Framebuffer(w / 16, h / 16, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 16, h / 16, PixelInternalFormat.R11fG11fB10f));

            if (StarBuffer != null) StarBuffer.Dispose();
            StarBuffer = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f));

            if (HUDBuffer != null) HUDBuffer.Dispose();
            HUDBuffer = new Framebuffer(Width, Height, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(Width, Height, PixelInternalFormat.Rgba8));

            if (OcclusionBuffer != null) OcclusionBuffer.Dispose();
            OcclusionBuffer = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R8));

            if (TAABuffer0 != null) TAABuffer0.Dispose();
            TAABuffer0 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 },
                new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f).SetFilter<Texture2D>(TextureMinFilter.Linear, TextureMagFilter.Linear),
                new Texture2D(w, h, PixelInternalFormat.R32f).SetFilter<Texture2D>(TextureMinFilter.Linear, TextureMagFilter.Linear));
            if (TAABuffer1 != null) TAABuffer1.Dispose();
            TAABuffer1 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 },
                new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f).SetFilter<Texture2D>(TextureMinFilter.Linear, TextureMagFilter.Linear),
                new Texture2D(w, h, PixelInternalFormat.R32f).SetFilter<Texture2D>(TextureMinFilter.Linear, TextureMagFilter.Linear));

            if (DOFWeightBuffer != null) DOFWeightBuffer.Dispose();
            DOFWeightBuffer = new Framebuffer(w / 2, h / 2, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 2, h / 2, PixelInternalFormat.R16f));
        }

        private void InitShaders()
        {
            ShaderManager.Instance.RegisterFragmentShader("GBuffer", "3D/GBuffer/GBuffer.vert", "3D/GBuffer/GBuffer.frag").Compile();
            ShaderManager.Instance.RegisterFragmentShader("ShadowMap", "3D/GBuffer/ShadowMap.vert", "3D/GBuffer/Empty.frag").Compile();

            ShaderManager.Instance.RegisterFragmentShader("ShadowFilter", "3D/Deferred/Deferred.vert", "3D/Deferred/Shadow.frag").Compile()
                .SetInt("depthTex", 0)
                .SetInt("normalTex", 1)
                .SetIntArr("shadowTex[0]", Enumerable.Range(2, 2 + SHADOW_CASCADES).ToArray());

            ShaderManager.Instance.RegisterFragmentShader("Lighting", "3D/Deferred/Deferred.vert", "3D/Deferred/Lighting.frag")
                .Define("RAYTRACE", (bool)OptionManager.Instance["Graphics", "RTGI Enabled"])
                .Define("SSAO_ENABLED", (bool)OptionManager.Instance["Graphics", "SSAO Enabled"])
                .Define("SHADOWS_ENABLED", (bool)OptionManager.Instance["Graphics", "Shadows Enabled"])
                .Compile()
                .SetInt("depthTex", 0)
                .SetInt("colorTex", 1)
                .SetInt("normalTex", 2)
                .SetInt("shadowTex", 3)
                .SetInt("skyTex", 4)
                .SetInt("starTex", 5)
                .SetInt("fogTex", 6)
                .SetInt("occlusionTex", 7)
                .SetInt("giTex", 8)
                .SetInt("giNTex", 9);

            ShaderManager.Instance.RegisterFragmentShader("RTGI", "3D/Deferred/Deferred.vert", "3D/Deferred/RTGI.frag")
                .Compile()
                .SetInt("data", 0)
                .SetInt("depthTex", 1)
                .SetInt("normalTex", 2)
                .SetInt("memoryTex", 3)
                .SetInt("SIZE", 512);

            ShaderManager.Instance.RegisterFragmentShader("Occlusion", "3D/Deferred/Deferred.vert", "3D/Deferred/SSAO.frag")
                .Define("SSAO_SAMPLES", (int)OptionManager.Instance["Graphics", "SSAO Quality"])
                .Compile()
                .SetInt("depthTex", 0)
                .SetInt("normalTex", 1);

            //ShaderManager.Instance.RegisterFragmentShader("Volumetrics", "3D/Deferred/Deferred.vert", "3D/Deferred/Volumetrics.frag").Compile()
            //    .SetInt("depthTex", 0)
            //    .SetInt("normalTex", 1)
            //    .SetIntArr("shadowTex[0]", Enumerable.Range(2, 2 + SHADOW_CASCADES).ToArray());

            ShaderManager.Instance.RegisterFragmentShader("TAA", "3D/Deferred/Deferred.vert", "3D/Deferred/TAA.frag").Compile()
                .SetInt("depthTex", 0)
                .SetInt("colorTex0", 1)
                .SetInt("colorTex1", 2)
                .SetInt("depthTex1", 3);

            ShaderManager.Instance.RegisterFragmentShader("Bloom", "3D/Deferred/Deferred.vert", "3D/Deferred/Bloom.frag").Compile()
                .SetInt("colortex0", 0);

            ShaderManager.Instance.RegisterFragmentShader("BlurDownsample", "3D/Deferred/Deferred.vert", "3D/Deferred/BlurDownSample.frag").Compile()
                .SetInt("colortex0", 0);

            ShaderManager.Instance.RegisterFragmentShader("BlurUpsample", "3D/Deferred/Deferred.vert", "3D/Deferred/BlurUpSample.frag").Compile()
                .SetInt("colortex0", 0);

            ShaderManager.Instance.RegisterFragmentShader("Final3D", "3D/Deferred/Deferred.vert", "3D/Final3D.frag").Compile()
                .SetInt("colortex0", 0)
                .SetInt("colortex1", 1);

            ShaderManager.Instance.RegisterFragmentShader("Final2D", "3D/Deferred/Deferred.vert", "Final2D.frag").Compile()
                .SetInt("colortex0", 0)
                .SetInt("colortex1", 1);

            ShaderManager.Instance.RegisterFragmentShader("MotionBlur", "3D/Deferred/Deferred.vert", "3D/Deferred/MotionBlur.frag").Compile()
                .SetInt("colortex0", 0)
                .SetInt("colortex1", 1);

            ShaderManager.Instance.RegisterFragmentShader("Tonemapping", "3D/Deferred/Deferred.vert", "3D/Deferred/Tonemapper.frag").Compile()
                .SetInt("colorTex", 0);

            ShaderManager.Instance.RegisterFragmentShader("Sky", "3D/Deferred/Deferred.vert", "3D/Deferred/Sky.frag").Compile();
            ShaderManager.Instance.RegisterFragmentShader("Stars", "3D/Deferred/Deferred.vert", "3D/Deferred/Stars.frag").Compile();

            ShaderManager.Instance.RegisterFragmentShader("DOFBlur", "3D/Deferred/Deferred.vert", "3D/Deferred/DOFBlur.frag").Compile()
                .SetInt("weightTex", 0)
                .SetInt("colorTex", 1)
                .SetInt("depthTex", 2);

            ShaderManager.Instance.RegisterFragmentShader("DOFWeight", "3D/Deferred/Deferred.vert", "3D/Deferred/DOFWeight.frag").Compile()
                .SetInt("depthTex", 0);

            ShaderManager.Instance.RegisterFragmentShader("Sharpen", "3D/Deferred/Deferred.vert", "3D/Deferred/Sharpen.frag")
                .Define("AMOUNT", (float)OptionManager.Instance["Graphics", "Sharpness"])
                .Compile()
                .SetInt("colorTex", 0);

            ShaderManager.Instance.RegisterFragmentShader("UI", "2D/Passtrough.vert", "2D/Passtrough.frag").Compile();

            ShaderManager.Instance.RegisterComputeShader("Luminance", "3D/Deferred/Luminance.comp").Compile();
            ShaderManager.Instance.RegisterComputeShader("LuminanceSmooth", "3D/Deferred/LuminanceSmooth.comp").Compile();
        }

        //public static ConcurrentDictionary<Vector3I, VoxelOctree> rtgiGeometry = new ConcurrentDictionary<Vector3I, VoxelOctree>();
        //Texture3D rtgiData;

        private void SetupPipeline()
        {
            renderActions.Clear();

            if (OptionManager.Instance["Graphics", "Shadows Enabled"])
            {
                Texture[] depthColorShadowTex = new Texture[SHADOW_CASCADES + 2];
                depthColorShadowTex[0] = GBuffer.depthTex0;
                depthColorShadowTex[1] = GBuffer.colorTex[1];
                for (int i = 0; i < SHADOW_CASCADES; i++)
                {
                    depthColorShadowTex[i + 2] = ShadowBuffers[i].depthTex0;
                }

                renderActions.Add(new StartProfilerSectionAction("Shadow Filter"));
                renderActions.Add(new ShaderPass(ShaderManager.Instance["ShadowFilter"], ShadowBuffer, depthColorShadowTex));
            }

            renderActions.Add(new StartProfilerSectionAction("Sky"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["Sky"], SkyBuffer));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["Stars"], StarBuffer));
            //renderActions.Add(new StartProfilerSectionAction("Volumetrics"));
            //renderActions.Add(new ShaderPass(ShaderManager.Instance["Volumetrics"], VolumetricBuffer, depthColorShadowTex));

            if (OptionManager.Instance["Graphics", "SSAO Enabled"])
            {
                renderActions.Add(new StartProfilerSectionAction("Occlusion"));
                renderActions.Add(new ShaderPass(ShaderManager.Instance["Occlusion"], OcclusionBuffer, GBuffer.depthTex0, GBuffer.colorTex[1]));
            }

            if (OptionManager.Instance["Graphics", "RTGI Enabled"])
            {
                //byte[] data = new byte[512 * 512 * 512];
                //uint[] temp = new uint[128];
                //for (int chx = 0; chx < 4; chx++)
                //{
                //    for (int chz = 0; chz < 4; chz++)
                //    {
                //        for (int chy = 0; chy < 4; chy++)
                //        {
                //            if (rtgiGeometry.ContainsKey(new(chx, chy, chz)))
                //            {
                //                VoxelOctree octree = rtgiGeometry[new(chx, chy, chz)];
                //                for (int x = 0; x < 128; x++)
                //                {
                //                    for (int z = 0; z < 128; z++)
                //                    {
                //                        octree.GetRow(x, z, 128, temp);

                //                        for (int y = 0; y < 128; y++)
                //                        {
                //                            data[((x + chx * 128) * 512 + (z + chz * 128)) * 512 + y + chy * 128] = (byte)(temp[y] != 0 ? Color.To7Bit(temp[y]) | 0x01 : 0);
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}
                //rtgiData.UploadData(data, PixelFormat.RedInteger, PixelType.UnsignedByte);

                //renderActions.Add(new StartProfilerSectionAction("RTGI"));
                //renderActions.Add(new ShaderPass(ShaderManager.Instance["RTGI"], GIBuffer0,
                //    rtgiData,
                //    GBuffer.depthTex0,
                //    GBuffer.colorTex[1],
                //    GIBuffer0.colorTex[1])
                //);

                //renderActions.Add(new StartProfilerSectionAction("Lighting"));
                //renderActions.Add(new ShaderPass(ShaderManager.Instance["Lighting"], RGB11BitBuffer0,
                //    GBuffer.depthTex0, GBuffer.colorTex[0], GBuffer.colorTex[1],
                //    ShadowBuffer.colorTex[0],
                //    SkyBuffer.colorTex[0],
                //    StarBuffer.colorTex[0],
                //    VolumetricBuffer.colorTex[0],
                //    OcclusionBuffer.colorTex[0],
                //    GIBuffer0.colorTex[0],
                //    GIBuffer0.colorTex[1])
                //);
            }
            else
            {
                renderActions.Add(new ShaderPass(ShaderManager.Instance["Lighting"], RGB11BitBuffer0,
                    GBuffer.depthTex0, GBuffer.colorTex[0], GBuffer.colorTex[1],
                    ShadowBuffer.colorTex[0],
                    SkyBuffer.colorTex[0],
                    StarBuffer.colorTex[0],
                    VolumetricBuffer.colorTex[0],
                    OcclusionBuffer.colorTex[0])
                );
            }


            PingPongFramebuffer taaPingPongFramebuffer = new PingPongFramebuffer(TAABuffer0, TAABuffer1);
            PingPongTexture taaPingPongTexture = new PingPongTexture(TAABuffer1.colorTex[0], TAABuffer0.colorTex[0]);
            PingPongTexture taaPingPongTextureD = new PingPongTexture(TAABuffer1.colorTex[1], TAABuffer0.colorTex[1]);
            renderActions.Add(new StartProfilerSectionAction("TAA"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["TAA"], taaPingPongFramebuffer, GBuffer.depthTex0, RGB11BitBuffer0.colorTex[0], taaPingPongTexture, taaPingPongTextureD));
            renderActions.Add(new SwapTexturesAction(taaPingPongTexture));
            renderActions.Add(new SwapTexturesAction(taaPingPongTextureD));
            renderActions.Add(new SwapFramebuffersAction(taaPingPongFramebuffer));
            renderActions.Add(new StartProfilerSectionAction("Tonemapping"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["Tonemapping"], RGB11BitBuffer0, taaPingPongTexture));
            renderActions.Add(new StartProfilerSectionAction("DOF"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["DOFWeight"], DOFWeightBuffer, GBuffer.depthTex0));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["DOFBlur"], RGB11BitBuffer1, DOFWeightBuffer.colorTex[0], RGB11BitBuffer0.colorTex[0], GBuffer.depthTex0));
            renderActions.Add(new StartProfilerSectionAction("Sharpen"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["Sharpen"], RGB11BitBuffer0, RGB11BitBuffer1.colorTex[0]));
            renderActions.Add(new StartProfilerSectionAction("Bloom"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["Bloom"], BloomBuffer0, RGB11BitBuffer0.colorTex[0]));

            for (int i = 0; i < 10; i++)
            {
                renderActions.Add(new ShaderPass(ShaderManager.Instance["BlurDownsample"], BloomBuffer1, BloomBuffer0.colorTex[0]));
                renderActions.Add(new ShaderPass(ShaderManager.Instance["BlurDownsample"], BloomBuffer0, BloomBuffer1.colorTex[0]));
            }

            for (int i = 0; i < 10; i++)
            {
                renderActions.Add(new ShaderPass(ShaderManager.Instance["BlurUpsample"], BloomBuffer1, BloomBuffer0.colorTex[0]));
                renderActions.Add(new ShaderPass(ShaderManager.Instance["BlurUpsample"], BloomBuffer0, BloomBuffer1.colorTex[0]));
            }

            renderActions.Add(new StartProfilerSectionAction("Compositing"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["Final3D"], RGB8BtBuffer, RGB11BitBuffer0.colorTex[0], BloomBuffer0.colorTex[0]));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["Final2D"], null, RGB8BtBuffer.colorTex[0], HUDBuffer.colorTex[0]));
            renderActions.Add(new EndProfilerSectionAction());
        }


        // don't allocate memory pointlessly every frame
        private static Vector4[] corners = new Vector4[8];
        private void RecalculateFrustumCorners(Matrix4x4 projMatrix, Matrix4x4 viewMatrix)
        {
            Matrix4x4 mat;
            Matrix4x4.Invert(viewMatrix * projMatrix, out mat);

            corners[0] = Vector4.Transform(new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), mat);
            corners[1] = Vector4.Transform(new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), mat);
            corners[2] = Vector4.Transform(new Vector4(-1.0f, 1.0f, -1.0f, 1.0f), mat);
            corners[3] = Vector4.Transform(new Vector4(-1.0f, 1.0f, 1.0f, 1.0f), mat);
            corners[4] = Vector4.Transform(new Vector4(1.0f, -1.0f, -1.0f, 1.0f), mat);
            corners[5] = Vector4.Transform(new Vector4(1.0f, -1.0f, 1.0f, 1.0f), mat);
            corners[6] = Vector4.Transform(new Vector4(1.0f, 1.0f, -1.0f, 1.0f), mat);
            corners[7] = Vector4.Transform(new Vector4(1.0f, 1.0f, 1.0f, 1.0f), mat);
        }

        private Matrix4x4 RenderShadowMapLayer(DepthAttachedFramebuffer shadowBuffer, float n, float f)
        {
            GL.Disable(EnableCap.CullFace);
            RecalculateFrustumCorners(Matrix4x4.CreatePerspectiveFieldOfView(OpenTK.Mathematics.MathHelper.DegreesToRadians(Camera.Fov), Camera.AspectRatio, n, f), viewMatrix);
            Vector4 center = new Vector4();
            foreach (var c in corners)
            {
                center += c;
            }

            center /= corners.Length;
            Vector3 cn = new Vector3(center.X, center.Y, center.Z);
            Matrix4x4 look = Matrix4x4.CreateLookAt(cn + SunPosition, cn, new Vector3(0, 1, 0));

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

            ShaderManager.Instance["ShadowMap"].SetMatrix4("lightSpaceMatrix", lsm);

            GL.Viewport(0, 0, shadowBuffer.Width, shadowBuffer.Height);
            shadowBuffer.Use();
            GL.Clear(ClearBufferMask.DepthBufferBit);

            shadowLayer.Render(Camera, look * lightProjection);
            SceneRenderer.Instance.Render((FragmentShader)ShaderManager.Instance["ShadowMap"]);

            GL.Enable(EnableCap.CullFace);
            return lsm;
        }

        private void RenderShadowMap()
        {
            shadowMatrices[0] = RenderShadowMapLayer(ShadowBuffers[0], shadowNears[0], shadowFars[0]);
            if (FrameIndex % 2 == 0)
            {
                shadowMatrices[1] = RenderShadowMapLayer(ShadowBuffers[1], shadowNears[1], shadowFars[1]);
            }
            else
            {
                shadowMatrices[2] = RenderShadowMapLayer(ShadowBuffers[2], shadowNears[2], shadowFars[2]);
            }

            ShaderManager.Instance["ShadowFilter"]
                .SetVector2("taaOffset", taaJitter)
                .SetMatrix4Arr("matrices[0]", shadowMatrices)
                .SetFloatArr("cuts[0]", shadowFars)
                .SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d))
                .SetMatrix4("projection", viewProjInvMatrix);
        }

        private void SetUniforms()
        {
            // IWorld should be providing this...
            Vector3 sunColor = new Vector3(9f, 6.3f, 5.5f);
            Vector3 c = new(.055f, .130f, .224f);
            float stuff = MathF.Pow(MathUtil.Smoothstep(0f, 1f, SunPosition.Y / 2 + .5f), 24f) * 80f;
            Vector3 col = new(MathF.Pow(stuff, c.X), MathF.Pow(stuff, c.Y), MathF.Pow(stuff, c.Z));
            Vector3 sky = col / 10f + c / 20f;

            ShaderManager.Instance["ShadowMap"]
                .SetVector2("taaOffset", taaJitter)
                .SetMatrix4("view", viewMatrix)
                .SetMatrix4("projection", projMatrix);

            if (OptionManager.Instance["Graphics", "RTGI Enabled"])
            {
                ShaderManager.Instance["RTGI"].SetVector2("taaOffset", taaJitter)
                    .SetVector2("wh", new Vector2(GIBuffer0 != null ? GIBuffer0.Width : 0, GIBuffer0 != null ? GIBuffer0.Height : 0))
                    .SetVector3("skyLight", sky)
                    .SetVector3("sunLight", sunColor)
                    .SetMatrix4("projection", viewProjInvMatrix)
                    .SetMatrix4("_projection", viewProjMatrix)
                    .SetMatrix4("projectionPrev", prevProjMatrix)
                    .SetMatrix4("viewMatrix", viewMatrix)
                    .SetMatrix4("projMatrix", projMatrix)
                    .SetVector3("position", Camera.Position)
                    .SetVector3("viewDir", Camera.Front)
                    .SetVector3("sunDir", SunPosition)
                    .SetFloat("time", (float)(TimeUtil.Unix() / 5000d % 1d));
            }

            ShaderManager.Instance["GBuffer"].SetMatrix4("view", viewMatrix)
                .SetMatrix4("projection", projMatrix)
                .SetVector3("viewPos", Camera.Position)
                .SetVector3("viewDir", Camera.Front)
                .SetVector2("taaOffset", taaJitter);

            ShaderManager.Instance["Lighting"].SetVector3("skyLight", sky)
                .SetVector3("viewPos", Camera.Position)
                .SetVector3("sun.position", SunPosition)
                .SetVector3("sun.color", sunColor)
                .SetMatrix4("projection", viewProjInvMatrix)
                .SetInt("giW", GIBuffer0 != null ? GIBuffer0.Width : 0)
                .SetInt("giH", GIBuffer0 != null ? GIBuffer0.Height : 0)
                .SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 3600d));

            ShaderManager.Instance["Final3D"].SetFloat("aspect", AspectRatio)
                .SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));

            ShaderManager.Instance["Sky"].SetMatrix4("projection", projInvMatrix)
                .SetMatrix4("viewMatrix", viewMatrix)
                .SetVector3("viewPos", Camera.Position)
                .SetVector3("sun_dir", SunPosition)
                .SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 3600d));

            ShaderManager.Instance["Stars"].SetMatrix4("projection", projInvMatrix)
                .SetMatrix4("viewMatrix", viewMatrix)
                .SetVector3("sun_dir", SunPosition)
                .SetFloat("time", (float)(TimeUtil.Unix() / 100000d % 3600d));

            //ShaderManager.Instance["Volumetrics"].SetVector3("viewPos", Camera.Position)
            //    .SetVector3("sun.color", sunColor)
            //    .SetVector3("sun.position", SunPosition)
            //    .SetMatrix4("projection", viewProjInvMatrix)
            //    .SetFloat("time", (float)(TimeUtil.Unix() / 5000d % 86400d))
            //    .SetMatrix4Arr("matrices[0]", shadowMatrices)
            //    .SetFloatArr("cuts[0]", shadowFars)
            //    .SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));

            ShaderManager.Instance["Occlusion"].SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d))
                .SetMatrix4("_projection", viewProjMatrix)
                .SetMatrix4("projectionPrev", prevProjMatrix)
                .SetMatrix4("projection", viewProjInvMatrix);

            ShaderManager.Instance["Sharpen"].SetFloat("width", RGB11BitBuffer0.Width)
                .SetFloat("height", RGB11BitBuffer0.Height);

            ShaderManager.Instance["DOFBlur"].SetFloat("aspectRatio", AspectRatio);

            ShaderManager.Instance["Tonemapping"].SetFloat("width", RGB11BitBuffer0.Width)
                .SetFloat("height", RGB11BitBuffer0.Height)
                .SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));


            float blurRadius = .001f * Width * (float)OptionManager.Instance["Graphics", "3D Resolution Scale"];
            ShaderManager.Instance["BlurDownsample"].SetFloat("tw", 1f / BloomBuffer0.Width)
                .SetFloat("th", 1f / BloomBuffer0.Height)
                .SetFloat("radius", blurRadius);

            ShaderManager.Instance["BlurUpsample"].SetFloat("tw", 1f / BloomBuffer0.Width)
                .SetFloat("th", 1f / BloomBuffer0.Height)
                .SetFloat("radius", blurRadius);

            ShaderManager.Instance["TAA"].SetInt("width", GBuffer.Width)
                .SetInt("height", GBuffer.Height)
                .SetMatrix4("projectionPrev", prevViewProjMatrix)
                .SetMatrix4("projection", viewProjInvMatrix)
                .SetVector2("taaOffset", taaJitter);

        }

        public void RenderFrame(double frameTimeMillis)
        {
            GPUProfilter.Instance.BeginFrame();

            FrameTimeMillis = frameTimeMillis;
            FrameTimeAvg = (FrameTimeAvg * 99 + frameTimeMillis) / 100.0F;

            FrameIndex++;

            float haltonX = 2.0f * MathUtil.HaltonSequence(FrameIndex % 243 + 1, 2) - 1.0f;
            float haltonY = 2.0f * MathUtil.HaltonSequence(FrameIndex % 243 + 1, 3) - 1.0f;
            taaJitter = new Vector2(haltonX, haltonY) / new Vector2(GBuffer.Width, GBuffer.Height);

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.AlphaTest);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            CameraPositionProvider.Provide(Camera);


            prevViewMatrix = viewMatrix;
            viewMatrix = Camera.GetViewMatrix();
            Matrix4x4.Invert(viewMatrix, out viewInvMatrix);

            prevProjMatrix = projMatrix;
            projMatrix = Camera.GetProjectionMatrix();
            Matrix4x4.Invert(projMatrix, out projInvMatrix);

            prevViewProjMatrix = viewProjMatrix;
            viewProjMatrix = viewMatrix * projMatrix;
            Matrix4x4.Invert(viewProjMatrix, out viewProjInvMatrix);

            SetUniforms();

            GPUProfilter.Instance.StartSection("Main Layer");
            mainLayer.Render(Camera, default);
            GBuffer.Use();
            GL.Viewport(0, 0, GBuffer.Width, GBuffer.Height);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            SceneRenderer.Instance.Render((FragmentShader)ShaderManager.Instance["GBuffer"]);

            if (OptionManager.Instance["Graphics", "Shadows Enabled"])
            {
                GPUProfilter.Instance.StartSection("Shadow Layer");
                RenderShadowMap();
            }


            GPUProfilter.Instance.StartSection("HUD Layer");
            HUDBuffer.Use();
            GL.Viewport(0, 0, HUDBuffer.Width, HUDBuffer.Height);

            GL.Enable(EnableCap.Blend);
            GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.Max);
            GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.Zero, BlendingFactorDest.Zero);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            UIRenderer.Instance.Render();
            UIRenderer.Instance.Flush();
            GL.Disable(EnableCap.Blend);
            GPUProfilter.Instance.EndSection();

            foreach (var shaderPass in renderActions)
            {
                shaderPass.Apply();
            }

            GPUProfilter.Instance.EndFrame();
        }
    }
}
