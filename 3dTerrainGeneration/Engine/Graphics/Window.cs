using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace _3dTerrainGeneration.Engine.Graphics
{
    internal class Window : NativeWindow
    {
        public Window() : base(new NativeWindowSettings() { WindowState = WindowState.Normal, Profile = ContextProfile.Compatability })
        {
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
        }
    }
}
