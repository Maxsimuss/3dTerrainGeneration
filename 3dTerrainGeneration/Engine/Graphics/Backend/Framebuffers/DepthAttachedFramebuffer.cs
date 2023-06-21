using _3dTerrainGeneration.Engine.Graphics.Backend.Textures;
using OpenTK.Graphics.OpenGL;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Framebuffers
{
    class DepthAttachedFramebuffer : Framebuffer
    {
        public Texture2D depthTex0;

        public DepthAttachedFramebuffer(int Width, int Height, DrawBuffersEnum[] drawBuffers, Texture2D depthTexture, params Texture2D[] textures) : base(Width, Height, drawBuffers, textures)
        {
            depthTex0 = depthTexture;
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTex0.Handle, 0);
        }

        public override void Dispose()
        {
            base.Dispose();
            depthTex0.Delete();
        }
    }
}
