using _3dTerrainGeneration.Engine.Audio;
using _3dTerrainGeneration.Engine.Graphics.UI.Text;
using _3dTerrainGeneration.Engine.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Components
{
    internal class Button : BaseComponent, IScreenInputHandler
    {
        public event OnClick Clicked;
        public delegate void OnClick();

        string text = "";

        Vector4 color;

        public Button(TextRenderer renderer, float x, float y, float width, float height, Vector4 color, string text)
        {
            this.renderer = renderer;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            this.color = color;
            this.text = text;
        }

        public override void Render()
        {
            UIRenderer.Instance.DrawRect(X, Y, Width, Height, color);
            renderer.DrawTextWithShadowCentered(X + Width / 2, Y + Height / 2, Height / 2, text);
        }

        private bool MouseOver(float x, float y)
        {
            return x > X && y > Y && x < X + Width && y < Y + Height;
        }

        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, Vector2 cursor)
        {
            if (!mouseState.IsButtonPressed(MouseButton.Left)) return false;

            if (!MouseOver(cursor.X, cursor.Y)) return false;
         
            AudioEngine.Instance.PlaySound("ClickConfirm");
            Clicked();

            return true;
        }
    }
}
