using OpenTK.Graphics.OpenGL;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Textures
{
    internal class PingPongTexture : Texture
    {
        public Texture Texture0, Texture1;
        public PingPongTexture(Texture texture0, Texture texture1)
        {
            this.Texture0 = texture0;
            this.Texture1 = texture1;
        }

        public override void ActiveBind(int unit = 0)
        {
            Texture0.ActiveBind(unit);
        }

        public void Swap()
        {
            Texture temp = Texture1;
            Texture1 = Texture0;
            Texture0 = temp;
        }

        public override Texture SetWrap(TextureWrapMode wrapMode)
        {
            Texture0.SetWrap(wrapMode);
            Texture1.SetWrap(wrapMode);

            return this;
        }
    }
}
