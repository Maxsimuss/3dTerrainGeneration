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

        public DepthAttachedFramebuffer(int Width, int Height, DrawBuffersEnum[] drawBuffers, Texture depthTexture, params Texture[] textures) : base(Width, Height, drawBuffers, textures)
        {
            depthTex0 = depthTexture;
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTex0.Handle, 0);
        }

        public override void Dispose()
        {
            base.Dispose();
            depthTex0.Dispose();
        }
    }
}
