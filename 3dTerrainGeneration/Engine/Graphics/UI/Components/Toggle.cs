using _3dTerrainGeneration.Engine.Input;
using _3dTerrainGeneration.Engine.Options;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Components
{
    internal class Toggle : BaseComponent, IScreenInputHandler
    {
        private BoolOption option;

        public Toggle(float x, float y, float size, BoolOption option)
        {
            X = x;
            Y = y;
            Width = size;
            Height = size;

            this.option = option;
        }
        private bool MouseOver(float x, float y)
        {
            return x > X && y > Y && x < X + Width && y < Y + Height;
        }

        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, Vector2 cursor)
        {
            if (MouseOver(cursor.X, cursor.Y) && mouseState.IsButtonPressed(MouseButton.Left))
            {
                option.Value = !(bool)option.Value;
                return true;
            }

            return false;
        }

        public override void Render()
        {
            //outline
            UIRenderer.Instance.DrawRect(X, Y, Width * .1f, Height, new Vector4(1));
            UIRenderer.Instance.DrawRect(X + Width * .9f, Y, Width * .1f, Height, new Vector4(1));
            UIRenderer.Instance.DrawRect(X, Y, Width, Height * .1f, new Vector4(1));
            UIRenderer.Instance.DrawRect(X, Y + Height * .9f, Width, Height * .1f, new Vector4(1));

            if ((bool)option.Value)
            {
                UIRenderer.Instance.DrawRect(X, Y, Width, Height, new Vector4(1));
            }
        }
    }
}
