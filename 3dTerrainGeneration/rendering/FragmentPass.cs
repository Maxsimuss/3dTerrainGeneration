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
        public static void Apply(FragmentShader shader, Framebuffer targetFramebuffer, params Texture[] sourceTextures)
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, targetFramebuffer == null ? 0 : targetFramebuffer.FBO);

            shader.Use();

            for (int i = 0; i < sourceTextures.Length; i++)
            {
                sourceTextures[i].Use(TextureUnit.Texture0 + i);

            }

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
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
