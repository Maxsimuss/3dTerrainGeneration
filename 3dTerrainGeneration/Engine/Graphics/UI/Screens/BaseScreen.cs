using _3dTerrainGeneration.Engine.Graphics.UI.Components;
using _3dTerrainGeneration.Engine.Graphics.UI.Text;
using System.Collections.Generic;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Screens
{
    internal abstract class BaseScreen
    {
        protected TextRenderer textRenderer = TextRenderer.Instance;
        protected List<BaseComponent> children = new List<BaseComponent>();

        protected float Width => 100 * GraphicsEngine.Instance.AspectRatio;

        public virtual bool FreeCursor => false;

        public virtual void Render()
        {
            foreach (var component in children)
            {
                component.Render();
            }
        }
    }
}
