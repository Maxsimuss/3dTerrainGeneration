using _3dTerrainGeneration.Engine.Input;
using _3dTerrainGeneration.Engine.Options;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Components
{
    internal class Slider : BaseComponent, IScreenInputHandler
    {
        private DoubleOption doubleOption;
        private bool beingDragged = false;

        private float headX => X - Height * .5f + Width * (float)doubleOption.ValuePercentage;

        private Func<double, double> valueMap = null;

        public Slider(float x, float y, float width, float height, DoubleOption doubleOption, Func<double, double> valueMap = null)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            this.doubleOption = doubleOption;
            this.valueMap = valueMap;
        }

        private bool MouseOverHead(float x, float y)
        {
            return x > headX && y > Y && x < headX + Height && y < Y + Height;
        }

        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, Vector2 cursor)
        {
            if (MouseOverHead(cursor.X, cursor.Y))
            {
                beingDragged = true;
            }

            if (beingDragged && !mouseState.IsButtonDown(MouseButton.Left))
            {
                beingDragged = false;
            }

            if (beingDragged)
            {
                float cursorX = Math.Clamp(cursor.X, X, X + Width);

                if (valueMap == null)
                {
                    doubleOption.ValuePercentage = (cursorX - X) / Width;
                }
                else
                {
                    doubleOption.ValuePercentage = valueMap((cursorX - X) / Width);
                }

                return true;
            }

            return false;
        }

        public override void Render()
        {
            //track
            UIRenderer.Instance.DrawRect(X, Y + Height * .25f, Width, Height * .5f, new Vector4(1));

            //head
            UIRenderer.Instance.DrawRect(headX, Y, Height, Height, new Vector4(248 / 255f, 101 / 255f, 101 / 255f, 1f));
        }
    }
}
