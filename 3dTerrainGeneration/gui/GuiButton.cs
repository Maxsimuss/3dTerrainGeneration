using _3dTerrainGeneration.rendering;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.gui
{
    public class GuiButton
    {
        public event OnClick Clicked;
        public delegate void OnClick();


        FontRenderer renderer;
        string text = "";
        float x, y;
        float width, height;
        Vector4 color;

        public GuiButton(FontRenderer renderer, float x, float y, float width, float height, Vector4 color, string text)
        {
            this.renderer = renderer;
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.color = color;
            this.text = text;
        }

        public void Render()
        {
            Renderer2D.DrawRect(x - width, y - height * renderer.aspectRatio, x + width, y + height * renderer.aspectRatio, color);
            renderer.DrawTextWithShadowCentered(x, y, .0375f, text);
        }

        public void MouseClicked(float x, float y)
        {
            if(x > this.x - width && x < this.x + width && y > this.y - height * renderer.aspectRatio && y < this.y + height * renderer.aspectRatio)
            {
                Window.Instance.SoundManager.PlaySound(audio.SoundType.ClickConfirm, false, 1, .05f);
                Clicked();
            }
        }
    }
}
