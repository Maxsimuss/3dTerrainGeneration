using _3dTerrainGeneration.Engine.Graphics;
using _3dTerrainGeneration.Engine.Graphics.UI;
using _3dTerrainGeneration.Engine.Graphics.UI.Screens;
using _3dTerrainGeneration.Engine.Options;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3dTerrainGeneration.Engine.Input
{
    internal class UserInputHandler
    {
        private HashSet<IEntityInputHandler> inputHandlers = new HashSet<IEntityInputHandler>();

        public UserInputHandler()
        {
            OptionManager.Instance.RegisterOption("Controls", "Mouse Sensitivity", new DoubleOption(0.01, 2, .15));
            OptionManager.Instance.RegisterOption("Controls", "Move Forward", new KeyboardOption(Keys.W));
            OptionManager.Instance.RegisterOption("Controls", "Move Backward", new KeyboardOption(Keys.S));
            OptionManager.Instance.RegisterOption("Controls", "Move Left", new KeyboardOption(Keys.A));
            OptionManager.Instance.RegisterOption("Controls", "Move Right", new KeyboardOption(Keys.D));
        }

        public void RegisterInputHandler(IEntityInputHandler handler)
        {
            inputHandlers.Add(handler);
        }

        public void UnregisterInputHandler(IEntityInputHandler handler)
        {
            inputHandlers.Remove(handler);
        }

        private bool WasKeyToggled(KeyboardState keyboardState, Keys key)
        {
            return keyboardState.IsKeyDown(key) && !keyboardState.WasKeyDown(key);
        }

        public void HandleInput(KeyboardState keyboardState, MouseState mouseState)
        {

            if (keyboardState.IsKeyPressed(Keys.R))
            {
                GraphicsEngine.Instance.Reload();
            }

            if (keyboardState.IsKeyPressed(Keys.O))
            {
                if (OptionsScreen.OpenInstance != null)
                {
                    UIRenderer.Instance.CloseScreen(OptionsScreen.OpenInstance);
                    OptionsScreen.OpenInstance = null;
                }
                else
                {
                    OptionsScreen.OpenInstance = (OptionsScreen)UIRenderer.Instance.OpenScreen(new OptionsScreen());
                }
            }

            if(UIRenderer.Instance.HandleInput(keyboardState, mouseState))
            {
                return;
            }


            InputState inputState = new InputState();

            inputState.Yaw = mouseState.Delta.X * (float)OptionManager.Instance["Controls", "Mouse Sensitivity"];
            inputState.Pitch = mouseState.Delta.Y * (float)OptionManager.Instance["Controls", "Mouse Sensitivity"];

            inputState.Movement.X += keyboardState.IsKeyDown(OptionManager.Instance["Controls", "Move Forward"]) ? 1 : 0;
            inputState.Movement.X -= keyboardState.IsKeyDown(OptionManager.Instance["Controls", "Move Backward"]) ? 1 : 0;
            inputState.Movement.Z -= keyboardState.IsKeyDown(OptionManager.Instance["Controls", "Move Left"]) ? 1 : 0;
            inputState.Movement.Z += keyboardState.IsKeyDown(OptionManager.Instance["Controls", "Move Right"]) ? 1 : 0;

            if (inputState.Movement.LengthSquared() != 0)
            {
                Vector3.Normalize(inputState.Movement);
            }

            foreach (var handler in inputHandlers)
            {
                handler.HandleInput(inputState);
            }
        }
    }
}
