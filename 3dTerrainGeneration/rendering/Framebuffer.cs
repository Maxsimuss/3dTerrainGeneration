using OpenTK.Graphics.OpenGL4;
using System;

namespace _3dTerrainGeneration.rendering
{
    public class Framebuffer
    {
        public int FBO, Width, Height;
        public Texture[] colorTex;

        public Framebuffer(int Width, int Height, params Texture[] textures)
        {
            this.Width = Width;
            this.Height = Height;

            FBO = GL.GenFramebuffer();
            Use();

            DrawBuffersEnum[] drawBuffersEnums = new DrawBuffersEnum[textures.Length];
            colorTex = new Texture[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                colorTex[i] = textures[i];
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, TextureTarget.Texture2D, colorTex[i].Handle, 0);
                drawBuffersEnums[i] = DrawBuffersEnum.ColorAttachment0 + i;
            }

            GL.DrawBuffers(textures.Length, drawBuffersEnums);
        }

        public void Use()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
        }

        public virtual void Dispose()
        {
            GL.DeleteFramebuffer(FBO);
            foreach (Texture texture in colorTex)
            {
                texture.Dispose();
            }
        }

        public virtual int UseTextures(int offset = 0)
        {
            for (int i = 0; i < colorTex.Length; i++)
            {
                colorTex[i].Use(TextureUnit.Texture0 + i + offset);
            }

            return colorTex.Length + offset;
        }
    }
}
