using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Input
{
    internal interface IScreenInputHandler
    {
        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, Vector2 cursor);
    }
}
