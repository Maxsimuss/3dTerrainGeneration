using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.Graphics.Backend;
using _3dTerrainGeneration.Engine.Graphics.Backend.Textures;
using System.Collections.Generic;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Screens
{
    internal class DebugHud : BaseScreen
    {
        public override void Render()
        {
            textRenderer.DrawTextWithShadowCentered(0, .9f, .05f, string.Format("TEXURE VRAM {0}MB", Texture.TotalBytesAllocated / 1024 / 1024));
            textRenderer.DrawTextWithShadowCentered(0, .8f, .05f, string.Format("GEOMETRY VRAM {0} / {1}MB", SceneRenderer.Instance.VramUsage / 1024 / 1024, SceneRenderer.Instance.VramAllocated / 1024 / 1024));
            textRenderer.DrawTextWithShadowCentered(0, .7f, .05f, string.Format("FRAME TIME AVG {0:0.00}MS", GraphicsEngine.Instance.FrameTimeAvg));

            textRenderer.DrawTextWithShadow(-1, .5f, .0125f, "Frame summary:");
            List<string> times = GPUProfilter.Instance.GetTimes();
            for (int i = 0; i < times.Count; i++)
            {
                textRenderer.DrawTextWithShadow(-1, .5f - (i + 1) * .025f, .0125f, times[i]);
            }
        }
    }
}
