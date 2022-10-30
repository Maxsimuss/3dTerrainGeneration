using OpenTK.Graphics.OpenGL;

namespace _3dTerrainGeneration.rendering
{
    class RenderBufferFramebuffer : Framebuffer
    {
        private int RBO;

        public RenderBufferFramebuffer(int Width, int Height, DrawBuffersEnum[] drawBuffers, params Texture2D[] textures) : base(Width, Height, drawBuffers, textures)
        {
            RBO = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, RBO);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, Width, Height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, RBO);
        }
    }
}
