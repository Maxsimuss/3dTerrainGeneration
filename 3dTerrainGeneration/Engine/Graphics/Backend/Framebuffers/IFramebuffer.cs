namespace _3dTerrainGeneration.Engine.Graphics.Backend.Framebuffers
{
    internal interface IFramebuffer
    {
        public int Width { get; }
        public int Height { get; }


        public void Use();
        public void Dispose();
        public int UseTextures(int offset = 0);
    }
}
