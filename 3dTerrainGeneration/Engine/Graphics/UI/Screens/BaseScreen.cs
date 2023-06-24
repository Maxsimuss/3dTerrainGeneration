using _3dTerrainGeneration.Engine.Graphics.UI.Components;
using _3dTerrainGeneration.Engine.Graphics.UI.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Screens
{
    internal abstract class BaseScreen
    {
        protected TextRenderer textRenderer = TextRenderer.Instance;
        protected List<BaseComponent> children = new List<BaseComponent>();

        public virtual void Render()
        {
            foreach (var component in children)
            {
                component.Render();
            }
        }
    }
}
