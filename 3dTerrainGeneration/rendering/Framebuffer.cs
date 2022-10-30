using OpenTK.Graphics.OpenGL;

namespace _3dTerrainGeneration.rendering
{
    public class Framebuffer
    {
        public int FBO, Width, Height;
        public Texture2D[] colorTex;

        public Framebuffer(int Width, int Height, DrawBuffersEnum[] drawBuffers, params Texture2D[] textures)
        {
            this.Width = Width;
            this.Height = Height;

            FBO = GL.GenFramebuffer();
            Use();

            colorTex = new Texture2D[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                colorTex[i] = textures[i];
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, TextureTarget.Texture2D, colorTex[i].Handle, 0);
            }

            GL.DrawBuffers(drawBuffers.Length, drawBuffers);
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
                texture.Delete();
            }
        }

        public virtual int UseTextures(int offset = 0)
        {
            for (int i = 0; i < colorTex.Length; i++)
            {
                colorTex[i].ActiveBind(TextureUnit.Texture0 + i + offset);
            }

            return colorTex.Length + offset;
        }
    }
}
