using _3dTerrainGeneration.Engine.Graphics.UI.Text;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Components
{
    internal abstract class BaseComponent
    {
        protected TextRenderer renderer;
        public float X, Y;
        public float Width, Weight;

        public abstract void Render();
    }
}
