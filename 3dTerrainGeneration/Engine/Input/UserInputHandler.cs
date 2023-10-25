using _3dTerrainGeneration.Engine.Graphics;
using _3dTerrainGeneration.Engine.Graphics.UI;
using _3dTerrainGeneration.Engine.Graphics.UI.Screens;
using _3dTerrainGeneration.Engine.Options;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Input
{
    internal class UserInputHandler
    {
        private HashSet<IEntityInputHandler> inputHandlers = new HashSet<IEntityInputHandler>();

        public UserInputHandler()
        {
            OptionManager.Instance.RegisterOption("Controls", "Mouse Sensitivity", 0.01, 2, .15, .01);
            OptionManager.Instance.RegisterOption("Controls", "Move Forward", Keys.W);
            OptionManager.Instance.RegisterOption("Controls", "Move Backward", Keys.S);
            OptionManager.Instance.RegisterOption("Controls", "Move Left", Keys.A);
            OptionManager.Instance.RegisterOption("Controls", "Move Right", Keys.D);
            OptionManager.Instance.RegisterOption("Controls", "Jump", Keys.Space);
            OptionManager.Instance.RegisterOption("Controls", "Sneak", Keys.LeftShift);
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

            if (UIRenderer.Instance.HandleInput(keyboardState, mouseState))
            {
                return;
            }


            InputState inputState = new InputState();

            inputState.Yaw = mouseState.Delta.X * (float)OptionManager.Instance["Controls", "Mouse Sensitivity"];
            inputState.Pitch = mouseState.Delta.Y * (float)OptionManager.Instance["Controls", "Mouse Sensitivity"];

            inputState.Left = mouseState.IsButtonDown(MouseButton.Button1);
            inputState.Right = mouseState.IsButtonDown(MouseButton.Button2);

            inputState.Movement.X += keyboardState.IsKeyDown(OptionManager.Instance["Controls", "Move Forward"]) ? 1 : 0;
            inputState.Movement.X -= keyboardState.IsKeyDown(OptionManager.Instance["Controls", "Move Backward"]) ? 1 : 0;

            inputState.Movement.Y += keyboardState.IsKeyDown(OptionManager.Instance["Controls", "Jump"]) ? 1 : 0;
            inputState.Movement.Y -= keyboardState.IsKeyDown(OptionManager.Instance["Controls", "Sneak"]) ? 1 : 0;

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
