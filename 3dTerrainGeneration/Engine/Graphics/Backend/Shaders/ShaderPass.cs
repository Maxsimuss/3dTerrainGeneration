using _3dTerrainGeneration.Engine.Graphics.Backend.Framebuffers;
using _3dTerrainGeneration.Engine.Graphics.Backend.RenderActions;
using _3dTerrainGeneration.Engine.Graphics.Backend.Textures;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Shaders
{
    internal class ShaderPass : IRenderAction
    {
        private static readonly float[] QUAD_VERTICES = new float[] {
                -1.0f,  1.0f,  0.0f, 1.0f,
                -1.0f, -1.0f,  0.0f, 0.0f,
                 1.0f, -1.0f,  1.0f, 0.0f,
                -1.0f,  1.0f,  0.0f, 1.0f,
                 1.0f, -1.0f,  1.0f, 0.0f,
                 1.0f,  1.0f,  1.0f, 1.0f
        };

        private static int quadVAO = -1;

        private Shader shader;
        private IFramebuffer targetFramebuffer;
        private Texture[] sourceTextures;

        private int offset = 0;

        private void SetupVAO()
        {
            if (quadVAO != -1) return;

            quadVAO = GL.GenVertexArray();
            int quadVBO = GL.GenBuffer();

            GL.BindVertexArray(quadVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * QUAD_VERTICES.Length, QUAD_VERTICES, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        }

        public ShaderPass(Shader shader, IFramebuffer targetFramebuffer, params Texture[] sourceTextures) : this(shader, targetFramebuffer, 0, sourceTextures)
        {
        }

        public ShaderPass(Shader shader, IFramebuffer targetFramebuffer, int offset, params Texture[] sourceTextures)
        {
            SetupVAO();

            this.shader = shader;
            this.targetFramebuffer = targetFramebuffer;
            this.offset = offset;
            this.sourceTextures = sourceTextures;
        }

        public void Apply()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.BindVertexArray(quadVAO);

            if(targetFramebuffer == null)
            {
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
                GL.Viewport(0, 0, GraphicsEngine.Instance.Width, GraphicsEngine.Instance.Height);
            }
            else
            {
                targetFramebuffer.Use();
                GL.Viewport(0, 0, targetFramebuffer.Width, targetFramebuffer.Height);
            }

            shader.Use();

            for (int i = 0; i < sourceTextures.Length; i++)
            {
                sourceTextures[i].ActiveBind(TextureUnit.Texture0 + i + offset);
            }

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            //for (int i = 0; i < sourceTextures.Length; i++)
            //{
            //    GL.ActiveTexture(TextureUnit.Texture0 + i);
            //    GL.BindTexture(TextureTarget.Texture2D, 0);
            //}
        }
    }
}
