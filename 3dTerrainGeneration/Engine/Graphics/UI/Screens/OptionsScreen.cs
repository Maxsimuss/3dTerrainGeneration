using _3dTerrainGeneration.Engine.Graphics.UI.Components;
using _3dTerrainGeneration.Engine.Input;
using _3dTerrainGeneration.Engine.Options;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Screens
{
    internal class OptionsScreen : BaseScreen, IScreenInputHandler
    {
        public static OptionsScreen OpenInstance = null;

        Vector2 cursor;

        string currentCategory = "";
        Dictionary<string, Option> options = new Dictionary<string, Option>();

        public OptionsScreen()
        {
            float y = 1 - .2f;
            foreach (var category in OptionManager.Instance.ListCategories())
            {
                currentCategory = category;

                Button button = new Button(textRenderer, -.7f, y, .2f, .05f, new Vector4(1, 0, 0, .5f), category);
                y -= .2f;

                button.Clicked += () =>
                {
                    options = OptionManager.Instance.ListOptionsForCategoty(category);
                    currentCategory = category;
                };


                children.Add(button);
            }
        }

        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, Vector2 cursor)
        {
            this.cursor = cursor;

            foreach (var child in children)
            {
                (child as IScreenInputHandler)?.HandleInput(keyboardState, mouseState, cursor);
            }

            return true;
        }

        public override void Render()
        {
            float asp = GraphicsEngine.Instance.AspectRatio;

            UIRenderer.Instance.DrawRect(-1, -1, 1, 1, new Vector4(0, 0, 0, .75f));
            base.Render();

            UIRenderer.Instance.DrawRect(cursor.X - .01f, cursor.Y - .01f * asp, cursor.X + .01f, cursor.Y + .01f * asp, new Vector4(1, 1, 1, 1));

            textRenderer.DrawTextWithShadowCentered(0, .8f, .04f, currentCategory);

            float y = .6f;
            foreach (var item in options)
            {
                textRenderer.DrawTextWithShadow(-.4f, y, .02f, string.Format("{0}: {1}", item.Key, item.Value.Value));
                y -= .1f;
            }
        }
    }
}
