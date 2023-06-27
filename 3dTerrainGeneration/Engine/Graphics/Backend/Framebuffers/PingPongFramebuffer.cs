namespace _3dTerrainGeneration.Engine.Graphics.Backend.Framebuffers
{
    internal class PingPongFramebuffer : IFramebuffer
    {
        private IFramebuffer framebuffer0, framebuffer1;
        private bool pingPong = true;

        public int Width => framebuffer0.Width;
        public int Height => framebuffer0.Height;

        public PingPongFramebuffer(IFramebuffer framebuffer0, IFramebuffer framebuffer1)
        {
            this.framebuffer0 = framebuffer0;
            this.framebuffer1 = framebuffer1;
        }

        public void Dispose()
        {
            framebuffer0.Dispose();
            framebuffer1.Dispose();
        }

        public void Swap()
        {
            pingPong = !pingPong;
        }

        public void Use()
        {
            if (pingPong)
            {
                framebuffer0.Use();
            }
            else
            {
                framebuffer1.Use();
            }
        }

        public int UseTextures(int offset = 0)
        {
            if (pingPong)
            {
                return framebuffer0.UseTextures(offset);
            }
            else
            {
                return framebuffer1.UseTextures(offset);
            }
        }
    }
}
