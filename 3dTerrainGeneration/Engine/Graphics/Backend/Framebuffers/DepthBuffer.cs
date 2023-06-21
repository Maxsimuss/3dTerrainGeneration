using OpenTK.Graphics.OpenGL;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Framebuffers
{
    class DepthBuffer : IFramebuffer
    {
        public int FBO, depthTex;

        public static int shadowRes = 1024;

        public int Width => throw new System.NotImplementedException();

        public int Height => throw new System.NotImplementedException();

        public DepthBuffer()
        {
            FBO = GL.GenFramebuffer();
            Use();

            depthTex = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, depthTex);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32f, shadowRes, shadowRes, 0, PixelFormat.DepthComponent, PixelType.Float, 0);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTex, 0);

            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);
        }

        public void Use()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public int UseTextures(int offset = 0)
        {
            throw new System.NotImplementedException();
        }
    }
}
