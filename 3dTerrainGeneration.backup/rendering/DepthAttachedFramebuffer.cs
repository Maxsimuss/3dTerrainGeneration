using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.rendering
{
    class DepthAttachedFramebuffer : Framebuffer
    {
        public Texture depthTex0;

        public DepthAttachedFramebuffer(int Width, int Height, PixelInternalFormat format = PixelInternalFormat.Rgb8) : base(Width, Height, format, 4)
        {
            depthTex0 = new Texture(Width, Height, PixelInternalFormat.DepthComponent24, PixelFormat.DepthComponent);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTex0.Handle, 0);
        }

        public override void UseTextures(int offset = 0)
        {
            depthTex0.Use(TextureUnit.Texture0);
            base.UseTextures(1);
        }
    }
}
