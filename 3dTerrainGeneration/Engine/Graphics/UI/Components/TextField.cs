using _3dTerrainGeneration.Engine.Audio;
using _3dTerrainGeneration.Engine.Graphics.UI.Text;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Components
{
    internal class TextField : BaseComponent
    {
        public string text = "";
        string placeholder;
        public bool Focused;
        int maxLen;
        float scale;

        public TextField(TextRenderer renderer, float x, float y, string placeholder)
        {
            this.renderer = renderer;
            this.placeholder = placeholder;
            X = x;
            Y = y;
            scale = .035f;
            maxLen = 16;
            Width = scale * maxLen / 2;
        }

        Vector4 white = new(1f);
        Vector4 gray = new(1f, 1f, 1f, .5f);

        public void Render()
        {


            if (text.Length == 0)
                renderer.DrawTextCentered(X, Y, scale, placeholder, gray);
            else
                renderer.DrawTextWithShadowCentered(X, Y, scale, text);
            UIRenderer.Instance.DrawRect(X - Width, Y - scale * GraphicsEngine.Instance.AspectRatio - .01f, X + Width, Y - scale * GraphicsEngine.Instance.AspectRatio, Focused ? white : gray);
        }

        public void Append(char c)
        {
            AudioEngine.Instance.PlaySound("ClickHigh");

            if (text.Length == maxLen)
            {
                return;
            }
            text += c;
        }

        public void Remove()
        {
            AudioEngine.Instance.PlaySound("ClickLow");

            if (text.Length == 0) return;
            text = text.Substring(0, text.Length - 1);
        }

        public void MouseClicked(float x, float y)
        {
            Focused = x > X - Width && x < X + Width && y > Y - .01f - scale * GraphicsEngine.Instance.AspectRatio && y < Y + .01f + scale * GraphicsEngine.Instance.AspectRatio;
        }
    }
}
