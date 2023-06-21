using _3dTerrainGeneration.Engine.Graphics.Backend.Framebuffers;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.RenderActions
{
    internal class SwapFramebuffersAction : IRenderAction
    {
        private PingPongFramebuffer framebuffer;

        public SwapFramebuffersAction(PingPongFramebuffer texture)
        {
            this.framebuffer = texture;
        }

        public void Apply()
        {
            framebuffer.Swap();
        }
    }
}
