using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Input
{
    internal interface IScreenInputHandler
    {
        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, Vector2 cursor);
    }
}
