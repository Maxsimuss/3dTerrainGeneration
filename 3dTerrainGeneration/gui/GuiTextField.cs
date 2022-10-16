using _3dTerrainGeneration.audio;
using _3dTerrainGeneration.rendering;
using System.Numerics;

namespace _3dTerrainGeneration.gui
{
    public class GuiTextField
    {
        FontRenderer renderer;
        public string text = "";
        string placeholder;
        public bool Focused;
        int maxLen;
        float x, y;
        float scale;
        public float width;

        public GuiTextField(FontRenderer renderer, float x, float y, string placeholder)
        {
            this.renderer = renderer;
            this.placeholder = placeholder;
            this.x = x;
            this.y = y;
            scale = .035f;
            maxLen = 16;
            width = scale * maxLen / 2;
        }

        Vector4 white = new(1f);
        Vector4 gray = new(1f, 1f, 1f, .5f);

        public void Render()
        {


            if (text.Length == 0)
                renderer.DrawTextCentered(x, y, scale, placeholder, gray);
            else
                renderer.DrawTextWithShadowCentered(x, y, scale, text);
            Renderer2D.DrawRect(x - width, y - scale * renderer.aspectRatio - .01f, x + width, y - scale * renderer.aspectRatio, Focused ? white : gray);
        }

        public void Append(char c)
        {
            Window.Instance.SoundManager.PlaySound(SoundType.ClickHigh, false, 1, .05f);
            if (text.Length == maxLen)
            {
                return;
            }
            text += c;
        }

        public void Remove()
        {
            Window.Instance.SoundManager.PlaySound(SoundType.ClickLow, false, 1, .05f);

            if (text.Length == 0) return;
            text = text.Substring(0, text.Length - 1);
        }

        public void MouseClicked(float x, float y)
        {
            Focused = x > this.x - width && x < this.x + width && y > this.y - .01f - scale * renderer.aspectRatio && y < this.y + .01f + scale * renderer.aspectRatio;
        }
    }
}
