using _3dTerrainGeneration.Engine.Graphics._3D;
using _3dTerrainGeneration.Engine.Graphics.Backend;
using _3dTerrainGeneration.Engine.Graphics.Backend.Textures;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Screens
{
    internal class DebugHud : BaseScreen
    {
        public static ConcurrentDictionary<string, string> DebugInfo = new ConcurrentDictionary<string, string>();

        public override void Render()
        {
            textRenderer.DrawTextWithShadowCentered(Width / 2, 5, 2.5f, string.Format("TEXURE VRAM {0}MB", Texture.TotalBytesAllocated / 1024 / 1024));
            textRenderer.DrawTextWithShadowCentered(Width / 2, 10, 2.5f, string.Format("GEOMETRY VRAM {0} / {1}MB", SceneRenderer.Instance.VramUsage / 1024 / 1024, SceneRenderer.Instance.VramAllocated / 1024 / 1024));
            textRenderer.DrawTextWithShadowCentered(Width / 2, 15, 2.5f, string.Format("FRAME TIME AVG {0:0.00}MS", GraphicsEngine.Instance.FrameTimeAvg));

            textRenderer.DrawTextWithShadow(0, 25, 1.25f, "Frame summary:");
            List<string> times = GPUProfilter.Instance.GetTimes();
            for (int i = 0; i < times.Count; i++)
            {
                textRenderer.DrawTextWithShadow(0, 30 + i * 2f, 1.25f, times[i]);
            }

            int line = 0;
            foreach (var item in DebugInfo)
            {
                textRenderer.DrawTextWithShadow(Width * .8f, 30 + line * 2f, 1.25f, item.Key + ": " + item.Value);
                line++;
            }
        }
    }
}
