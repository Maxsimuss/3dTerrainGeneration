using _3dTerrainGeneration.Engine.Graphics.Backend.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Screens
{
    internal class DebugHud : BaseScreen
    {
        public override void Render()
        {
            textRenderer.DrawTextWithShadowCentered(0, .9f, .05f, string.Format("TEXURE VRAM {0}MB", Texture.TotalBytesAllocated / 1024 / 1024));
            textRenderer.DrawTextWithShadowCentered(0, .8f, .05f, string.Format("FRAME TIME AVG {0:0.00}MS", GraphicsEngine.Instance.FrameTimeAvg));
        }
    }
}
