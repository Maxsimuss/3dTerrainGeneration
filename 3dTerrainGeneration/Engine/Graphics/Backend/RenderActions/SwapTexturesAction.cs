using _3dTerrainGeneration.Engine.Graphics.Backend.Textures;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.RenderActions
{
    internal class SwapTexturesAction : IRenderAction
    {
        private PingPongTexture texture;

        public SwapTexturesAction(PingPongTexture texture)
        {
            this.texture = texture;
        }

        public void Apply()
        {
            texture.Swap();
        }
    }
}
