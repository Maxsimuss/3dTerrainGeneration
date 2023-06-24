using _3dTerrainGeneration.Engine.Graphics.Backend.Framebuffers;
using _3dTerrainGeneration.Engine.Graphics.Backend.Shaders;
using _3dTerrainGeneration.Engine.Graphics.Backend.Textures;
using _3dTerrainGeneration.Engine.Graphics.UI;
using _3dTerrainGeneration.Engine.Graphics._3D;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using _3dTerrainGeneration.Engine.Util;
using _3dTerrainGeneration.Engine.Graphics.Backend.RenderActions;
using _3dTerrainGeneration.Engine.Graphics._3D.Cameras;
using _3dTerrainGeneration.Engine.Options;
using _3dTerrainGeneration.Engine.Graphics.Backend;

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

        private static readonly int shadowCascades = 3;

        private static readonly Vector2[] TAA_OFFSETS = new Vector2[] { new(-1, 1), new(0, 1), new(1, 1), new(-1, 0), new(0, 0), new(1, 0), new(-1, -1), new(0, -1), new(1, -1) };

        private ComputeShader LuminanceCompute, LuminanceSmoothCompute;

        private DepthAttachedFramebuffer GBuffer;
        private DepthAttachedFramebuffer[] ShadowBuffers;

        private Framebuffer RGB11BitBuffer0, RGB11BitBuffer1, VolumetricBuffer, ShadowBuffer, BloomBuffer0, BloomBuffer1, HUDBuffer, OcclusionBuffer, TAABuffer0, TAABuffer1, DOFWeightBuffer, GIBuffer0, GIBuffer1, RGB8BtBuffer;
        private Framebuffer SkyBuffer, StarBuffer;

        private List<IRenderAction> renderActions = new List<IRenderAction>();

        private Matrix4x4[] shadowMatrices = new Matrix4x4[shadowCascades];
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
            OptionManager.Instance.RegisterOption("Graphics", "3D Resolution Scale", new DoubleOption(.125, 2, 1));
            OptionManager.Instance.RegisterOption("Graphics", "Sharpness", new DoubleOption(0, 1, .25));
            OptionManager.Instance.RegisterOption("Graphics", "Shadow Resolution", new DoubleOption(512, 8196, 4096));
            OptionManager.Instance.RegisterOption("Graphics", "SSAO Quality", new DoubleOption(1, 16, 4));
            OptionManager.Instance.RegisterOption("Graphics", "RTGI Enabled", new BoolOption(false));
            OptionManager.Instance.RegisterOption("Graphics", "RTGI Resolution", new DoubleOption(1, 16, 1));
            OptionManager.Instance.RegisterOption("Graphics", "RTGI Quality", new DoubleOption(1, 16, 1));

            Console.WriteLine("OGL Version: {0}", GL.GetString(StringName.Version));
            Console.WriteLine("OGL Vendor: {0}", GL.GetString(StringName.Vendor));
            Console.WriteLine("OGL SL Version: {0}", GL.GetString(StringName.ShadingLanguageVersion));
            Console.WriteLine("OGL Renderer: {0}", GL.GetString(StringName.Renderer));
            Console.WriteLine("OGL Extensions: {0}", GL.GetString(StringName.Extensions));

            _debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);

            GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);
            //GL.Enable(EnableCap.DebugOutputSynchronous);

            Reload();
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
            GBuffer = new DepthAttachedFramebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2 },
                new Texture2D(w, h, PixelInternalFormat.DepthComponent32f, PixelFormat.DepthComponent),
                new Texture2D(w, h, PixelInternalFormat.Rgba8, PixelFormat.Rgba),
                new Texture2D(w, h, (PixelInternalFormat)All.Rgb565, PixelFormat.Rgb),
                new Texture2D(w, h, PixelInternalFormat.Rgba32f, PixelFormat.Rgba).SetFilter<Texture2D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest));

            if (OptionManager.Instance["Graphics", "RTGI Enabled"])
            {
                int giW = (int)OptionManager.Instance["Graphics", "RTGI Resolution"];
                int giH = (int)OptionManager.Instance["Graphics", "RTGI Resolution"];
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
            }

            int shadowResoluion = (int)OptionManager.Instance["Graphics", "Shadow Resolution"];
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
                ShadowBuffers[i] = new DepthAttachedFramebuffer(shadowResoluion, shadowResoluion, new DrawBuffersEnum[0],
                    new Texture2D(shadowResoluion, shadowResoluion, PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent).SetWrap(TextureWrapMode.ClampToBorder).SetBorderColor<Texture2D>(1, 1, 1, 1));
            }

            if (RGB11BitBuffer0 != null) RGB11BitBuffer0.Dispose();
            RGB11BitBuffer0 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb));

            if (RGB11BitBuffer1 != null) RGB11BitBuffer1.Dispose();
            RGB11BitBuffer1 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb));

            if (VolumetricBuffer != null) VolumetricBuffer.Dispose();
            VolumetricBuffer = new Framebuffer(w / 2, h / 2, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 2, h / 2, PixelInternalFormat.Rgba16f, PixelFormat.Rgba));

            if (ShadowBuffer != null) ShadowBuffer.Dispose();
            ShadowBuffer = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R8, PixelFormat.Red));

            if (RGB8BtBuffer != null) RGB8BtBuffer.Dispose();
            RGB8BtBuffer = new Framebuffer(Width, Height, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(Width, Height, PixelInternalFormat.Rgb8, PixelFormat.Rgb));

            if (BloomBuffer0 != null) BloomBuffer0.Dispose();
            BloomBuffer0 = new Framebuffer(w / 4, h / 4, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 4, h / 4, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb));

            if (BloomBuffer1 != null) BloomBuffer1.Dispose();
            BloomBuffer1 = new Framebuffer(w / 4, h / 4, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 4, h / 4, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb));

            if (SkyBuffer != null) SkyBuffer.Dispose();
            SkyBuffer = new Framebuffer(w / 16, h / 16, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 16, h / 16, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb));

            if (StarBuffer != null) StarBuffer.Dispose();
            StarBuffer = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb));

            if (HUDBuffer != null) HUDBuffer.Dispose();
            HUDBuffer = new Framebuffer(Width, Height, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(Width, Height, PixelInternalFormat.Rgba8, PixelFormat.Rgba));

            if (OcclusionBuffer != null) OcclusionBuffer.Dispose();
            OcclusionBuffer = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w, h, PixelInternalFormat.R8, PixelFormat.Red));

            if (TAABuffer0 != null) TAABuffer0.Dispose();
            TAABuffer0 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 }, 
                new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb).SetFilter<Texture2D>(TextureMinFilter.Linear, TextureMagFilter.Linear),
                new Texture2D(w, h, PixelInternalFormat.R32f, PixelFormat.Red).SetFilter<Texture2D>(TextureMinFilter.Linear, TextureMagFilter.Linear));
            if (TAABuffer1 != null) TAABuffer1.Dispose();
            TAABuffer1 = new Framebuffer(w, h, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 }, 
                new Texture2D(w, h, PixelInternalFormat.R11fG11fB10f, PixelFormat.Rgb).SetFilter<Texture2D>(TextureMinFilter.Linear, TextureMagFilter.Linear),
                new Texture2D(w, h, PixelInternalFormat.R32f, PixelFormat.Red).SetFilter<Texture2D>(TextureMinFilter.Linear, TextureMagFilter.Linear));

            if (DOFWeightBuffer != null) DOFWeightBuffer.Dispose();
            DOFWeightBuffer = new Framebuffer(w / 2, h / 2, new[] { DrawBuffersEnum.ColorAttachment0 }, new Texture2D(w / 2, h / 2, PixelInternalFormat.R16f, PixelFormat.Red));
        }

        private void InitShaders()
        {
            ShaderManager.Instance.RegisterFragmentShader("GBuffer", "gbuffer.vert", "gbuffer.frag").Compile();
            ShaderManager.Instance.RegisterFragmentShader("ShadowMap", "shadowmap.vert", "empty.frag").Compile();

            ShaderManager.Instance.RegisterFragmentShader("ShadowFilter", "post.vert", "shadow.frag").Compile()
                .SetInt("depthTex", 0)
                .SetInt("normalTex", 1)
                .SetIntArr("shadowTex[0]", Enumerable.Range(2, 2 + shadowCascades).ToArray());

            ShaderManager.Instance.RegisterFragmentShader("Lighting", "post.vert", "lighting.frag").Compile()
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

            ShaderManager.Instance.RegisterFragmentShader("RTGI", "post.vert", "rtgi.frag")
                .Compile()
                .SetInt("data", 0)
                .SetInt("depthTex", 1)
                .SetInt("normalTex", 2)
                .SetInt("SIZE", 512)
                .SetInt("memoryTex", 3)
                .SetInt("positionTex", 4);



            ShaderManager.Instance.RegisterFragmentShader("Occlusion", "post.vert", "occlusion.frag")
                .Define("SSAO_SAMPLES", (int)OptionManager.Instance["Graphics", "SSAO Quality"])
                .Compile()
                .SetInt("depthTex", 0)
                .SetInt("normalTex", 1);

            ShaderManager.Instance.RegisterFragmentShader("Volumetrics", "post.vert", "volumetrics.frag").Compile()
                .SetInt("depthTex", 0)
                .SetInt("normalTex", 1)
                .SetIntArr("shadowTex[0]", Enumerable.Range(2, 2 + shadowCascades).ToArray());

            ShaderManager.Instance.RegisterFragmentShader("TAA", "post.vert", "post/taa.frag").Compile()
                .SetInt("depthTex", 0)
                .SetInt("colorTex0", 1)
                .SetInt("colorTex1", 2)
                .SetInt("depthTex1", 3);

            ShaderManager.Instance.RegisterFragmentShader("Bloom", "post.vert", "post/bloom.frag").Compile()
                .SetInt("colortex0", 0);

            ShaderManager.Instance.RegisterFragmentShader("BlurDownsample", "post.vert", "post/downsample.frag").Compile()
                .SetInt("colortex0", 0);

            ShaderManager.Instance.RegisterFragmentShader("BlurUpsample", "post.vert", "post/upsample.frag").Compile()
                .SetInt("colortex0", 0);

            ShaderManager.Instance.RegisterFragmentShader("Final3D", "post.vert", "post/final3d.frag").Compile()
                .SetInt("colortex0", 0)
                .SetInt("colortex1", 1);

            ShaderManager.Instance.RegisterFragmentShader("Final2D", "post.vert", "post/final2d.frag").Compile()
                .SetInt("colortex0", 0)
                .SetInt("colortex1", 1);

            ShaderManager.Instance.RegisterFragmentShader("MotionBlur", "post.vert", "post/motionblur.frag").Compile()
                .SetInt("colortex0", 0)
                .SetInt("colortex1", 1);

            ShaderManager.Instance.RegisterFragmentShader("Tonemapping", "post.vert", "tonemapping.frag").Compile()
                .SetInt("colorTex", 0);

            ShaderManager.Instance.RegisterFragmentShader("Sky", "post.vert", "sky.frag").Compile();
            ShaderManager.Instance.RegisterFragmentShader("Stars", "post.vert", "stars.frag").Compile();

            ShaderManager.Instance.RegisterFragmentShader("DOFBlur", "post.vert", "dofblur.frag").Compile()
                .SetInt("weightTex", 0)
                .SetInt("colorTex", 1)
                .SetInt("depthTex", 2);

            ShaderManager.Instance.RegisterFragmentShader("DOFWeight", "post.vert", "dofweight.frag").Compile()
                .SetInt("depthTex", 0);

            ShaderManager.Instance.RegisterFragmentShader("Sharpen", "post.vert", "sharpen.frag")
                .Define("AMOUNT", (float)OptionManager.Instance["Graphics", "Sharpness"])
                .Compile()
                .SetInt("colorTex", 0);

            ShaderManager.Instance.RegisterFragmentShader("UI", "rect.vert", "rect.frag").Compile();

            ShaderManager.Instance.RegisterComputeShader("Luminance", "luminance.comp").Compile();
            ShaderManager.Instance.RegisterComputeShader("LuminanceSmooth", "luminancesmooth.comp").Compile();
        }

        private void SetupPipeline()
        {
            renderActions.Clear();

            Texture[] depthColorShadowTex = new Texture[shadowCascades + 2];
            depthColorShadowTex[0] = GBuffer.depthTex0;
            depthColorShadowTex[1] = GBuffer.colorTex[1];
            for (int i = 0; i < shadowCascades; i++)
            {
                depthColorShadowTex[i + 2] = ShadowBuffers[i].depthTex0;
            }

            renderActions.Add(new StartProfilerSectionAction("Shadow Filter"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["ShadowFilter"], ShadowBuffer, depthColorShadowTex));
            renderActions.Add(new StartProfilerSectionAction("Sky"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["Sky"], SkyBuffer));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["Stars"], StarBuffer));
            renderActions.Add(new StartProfilerSectionAction("Volumetrics"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["Volumetrics"], VolumetricBuffer, depthColorShadowTex));
            renderActions.Add(new StartProfilerSectionAction("Occlusion"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["Occlusion"], OcclusionBuffer, GBuffer.depthTex0, GBuffer.colorTex[1]));

            renderActions.Add(new StartProfilerSectionAction("Lighting"));
            if (OptionManager.Instance["Graphics", "RTGI Enabled"])
            {
                renderActions.Add(new ShaderPass(ShaderManager.Instance["Lighting"], RGB11BitBuffer0,
                  GBuffer.depthTex0, GBuffer.colorTex[0], GBuffer.colorTex[1],
                  ShadowBuffer.colorTex[0],
                  SkyBuffer.colorTex[0],
                  StarBuffer.colorTex[0],
                  VolumetricBuffer.colorTex[0],
                  OcclusionBuffer.colorTex[0],
                  GIBuffer0.colorTex[0],
                  GIBuffer0.colorTex[1])
              );
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


            PingPongFramebuffer pingPongFramebuffer = new PingPongFramebuffer(TAABuffer0, TAABuffer1);
            PingPongTexture pingPongTexture = new PingPongTexture(TAABuffer1.colorTex[0], TAABuffer0.colorTex[0]);
            PingPongTexture pingPongTextureD = new PingPongTexture(TAABuffer1.colorTex[1], TAABuffer0.colorTex[1]);
            renderActions.Add(new StartProfilerSectionAction("TAA"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["TAA"], pingPongFramebuffer, GBuffer.depthTex0, RGB11BitBuffer0.colorTex[0], pingPongTexture, pingPongTextureD));
            renderActions.Add(new SwapTexturesAction(pingPongTexture));
            renderActions.Add(new SwapTexturesAction(pingPongTextureD));
            renderActions.Add(new SwapFramebuffersAction(pingPongFramebuffer));
            renderActions.Add(new StartProfilerSectionAction("Tonemapping"));
            renderActions.Add(new ShaderPass(ShaderManager.Instance["Tonemapping"], RGB11BitBuffer0, pingPongTexture));
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
            for (int i = 0; i < shadowCascades; i++)
            {
                shadowMatrices[i] = RenderShadowMapLayer(ShadowBuffers[i], shadowNears[i], shadowFars[i]);
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
                    .SetVector2("wh", new Vector2(GIBuffer0.Width, GIBuffer0.Height))
                    .SetVector3("skyLight", sky)
                    .SetVector3("sunLight", sunColor)
                    .SetMatrix4("projection", viewProjInvMatrix)
                    .SetMatrix4("_projection", viewProjMatrix)
                    .SetMatrix4("projectionPrev", prevProjMatrix)
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

            ShaderManager.Instance["Volumetrics"].SetVector3("viewPos", Camera.Position)
                .SetVector3("sun.color", sunColor)
                .SetVector3("sun.position", SunPosition)
                .SetMatrix4("projection", viewProjInvMatrix)
                .SetFloat("time", (float)(TimeUtil.Unix() / 5000d % 86400d))
                .SetMatrix4Arr("matrices[0]", shadowMatrices)
                .SetFloatArr("cuts[0]", shadowFars)
                .SetFloat("time", (float)(TimeUtil.Unix() / 1000d % 1d));

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
            taaJitter = TAA_OFFSETS[FrameIndex % 9] * new Vector2(1f / GBuffer.Width, 1f / GBuffer.Height) * .5f;

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

            GPUProfilter.Instance.StartSection("Shadow Layer");
            RenderShadowMap();

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
