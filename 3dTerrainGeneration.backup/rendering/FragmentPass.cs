using _3dTerrainGeneration.world;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace _3dTerrainGeneration.rendering
{
    class FragmentPass
    {
        public int Order;
        private Framebuffer targetFramebuffer = null;
        private Shader shader;

        float Lerp(float firstFloat, float secondFloat, float by)
        {
            return firstFloat * (1 - by) + secondFloat * by;
        }

        public FragmentPass(string shaderName, int Width, int Height)
        {
            shader = new Shader("shaders/post.vert", shaderName);
            shader.SetInt("colortex0", 0);
            shader.SetInt("colortex1", 1);
            shader.SetInt("colortex2", 2);
            shader.SetInt("colortex3", 3);
            shader.SetInt("colortex4", 4);

            if (!shaderName.Contains("final"))
            {
                targetFramebuffer = new Framebuffer(Width, Height, PixelInternalFormat.Rgb16f, 2);
                string[] parts = shaderName.Split('_');
                Order = int.Parse(parts[parts.Length - 1].Split('.')[0]);
            } 
            else
            {
                Order = int.MaxValue;
            }
        }

        public Framebuffer Apply(Framebuffer framebuffer, float near, float far)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetFramebuffer == null ? 0 : targetFramebuffer.FBO);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.Use();
            shader.SetInt("width", framebuffer.Width);
            shader.SetInt("height", framebuffer.Height);
            shader.SetVector3("fogColor", new Vector3(World.fogColor.R, World.fogColor.G, World.fogColor.B));
            shader.SetFloat("zNear", near);
            shader.SetFloat("zFar", far);
            framebuffer.UseTextures();

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            return targetFramebuffer;
        }

        private static int quadVBO, quadVAO;
        private static float[] quadVertices = new float[] {  
            -1.0f,  1.0f,  0.0f, 1.0f,
            -1.0f, -1.0f,  0.0f, 0.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
            -1.0f,  1.0f,  0.0f, 1.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
             1.0f,  1.0f,  1.0f, 1.0f
        };

        public static void Init()
        {
            quadVAO = GL.GenVertexArray();
            quadVBO = GL.GenBuffer();

            GL.BindVertexArray(quadVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * quadVertices.Length, quadVertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        }

        public static void BeginPostStage()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.BindVertexArray(quadVAO);
        }
    }
}
