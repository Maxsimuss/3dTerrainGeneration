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
            Weight = height;
            this.color = color;
            this.text = text;
        }

        public override void Render()
        {
            UIRenderer.Instance.DrawRect(X - Width, Y - Weight * GraphicsEngine.Instance.AspectRatio, X + Width, Y + Weight * GraphicsEngine.Instance.AspectRatio, color);
            renderer.DrawTextWithShadowCentered(X, Y, .0375f, text);
        }

        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, Vector2 cursor)
        {
            float x = cursor.X;
            float y = cursor.Y;

            if (!mouseState.IsButtonPressed(MouseButton.Left)) return false;

            if (x > X - Width && x < X + Width && y > Y - Weight * GraphicsEngine.Instance.AspectRatio && y < Y + Weight * GraphicsEngine.Instance.AspectRatio)
            {
                AudioEngine.Instance.PlaySound("ClickConfirm");
                Clicked();

                return true;
            }

            return false;
        }
    }
}
