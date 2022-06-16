using OpenTK.Graphics.OpenGL4;
using System;

namespace _3dTerrainGeneration.rendering
{
    class Framebuffer
    {
        public int FBO, Width, Height;
        public Texture[] colorTex;

        public Framebuffer(int Width, int Height, PixelInternalFormat format = PixelInternalFormat.Rgb8, int colorAttachments = 1)
        {
            this.Width = Width;
            this.Height = Height;

            FBO = GL.GenFramebuffer();
            Use();

            colorTex = new Texture[colorAttachments];
            for (int i = 0; i < colorAttachments; i++)
            {
                colorTex[i] = new Texture(Width, Height, format);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, TextureTarget.Texture2D, colorTex[i].Handle, 0);
            }

            GL.DrawBuffers(colorAttachments, new DrawBuffersEnum[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3 });
        }

        public void Use()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
        }

        public virtual void UseTextures(int offset = 0)
        {
            for (int i = 0; i < colorTex.Length; i++)
            {
                colorTex[i].Use(TextureUnit.Texture0 + i + offset);
            }
        }
    }
}
