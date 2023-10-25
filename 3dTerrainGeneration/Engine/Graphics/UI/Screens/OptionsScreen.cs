using _3dTerrainGeneration.Engine.Graphics.UI.Components;
using _3dTerrainGeneration.Engine.Input;
using _3dTerrainGeneration.Engine.Options;
using Linearstar.Windows.RawInput.Native;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Screens
{
    internal class OptionsScreen : BaseScreen, IScreenInputHandler
    {
        public static OptionsScreen OpenInstance = null;

        string currentCategory = "";
        Dictionary<string, Option> options = new Dictionary<string, Option>();
        List<BaseComponent> components = new List<BaseComponent>();

        public override bool FreeCursor => true;

        public OptionsScreen()
        {
            float y = 5;
            foreach (var category in OptionManager.Instance.ListCategories())
            {
                currentCategory = category;

                Button button = new Button(textRenderer, 5, y, 30, 5, new Vector4(1, 0, 0, .5f), category);
                y += 7.5f;

                button.Clicked += () =>
                {
                    options = OptionManager.Instance.ListOptionsForCategoty(category);

                    float _y = 15;
                    components.Clear();
                    foreach (var option in options)
                    {
                        if(option.Value is DoubleOption)
                        {
                            components.Add(new Slider(Width - 60, _y, 50, 5, (DoubleOption)option.Value));
                            _y += 5;
                        }
                        if (option.Value is BoolOption)
                        {
                            components.Add(new Toggle(Width - 15, _y, 5, (BoolOption)option.Value));
                            _y += 5;
                        }
                    }
                    currentCategory = category;
                };


                children.Add(button);
            }
        }

        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, Vector2 cursor)
        {
            foreach (var child in children)
            {
                (child as IScreenInputHandler)?.HandleInput(keyboardState, mouseState, cursor);
            }

            foreach (var component in components)
            {
                (component as IScreenInputHandler)?.HandleInput(keyboardState, mouseState, cursor);
            }

            return true;
        }

        public override void Render()
        {
            UIRenderer.Instance.DrawRect(0, 0, Width, 100, new Vector4(0, 0, 0, .5f));
            base.Render();

            textRenderer.DrawTextWithShadow(40, 5, 5f, currentCategory);

            float y = 15;
            foreach (var item in options)
            {
                textRenderer.DrawTextWithShadow(40, y, 2.5f, string.Format("{0}: {1}", item.Key, item.Value.Value));
                y += 5f;
            }

            foreach (var component in components)
            {
                component.Render();
            }
        }
    }
}
