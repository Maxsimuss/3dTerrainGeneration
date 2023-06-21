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

        public abstract void Render();
    }
}
