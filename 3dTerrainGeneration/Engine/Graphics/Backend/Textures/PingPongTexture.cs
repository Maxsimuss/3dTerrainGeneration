using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Textures
{
    internal class PingPongTexture : Texture
    {
        public Texture Texture0, Texture1;
        public PingPongTexture(Texture texture0, Texture texture1) : base(texture0.TextureTarget)
        {
            this.Texture0 = texture0;
            this.Texture1 = texture1;
        }

        public override void ActiveBind(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget, Texture0.Handle);
        }

        public void Swap()
        {
            Texture temp = Texture1;
            Texture1 = Texture0;
            Texture0 = temp;
        }

        public override Texture SetWrap(TextureWrapMode wrapMode)
        {
            // yolo
            throw new NotImplementedException();
        }
    }
}
