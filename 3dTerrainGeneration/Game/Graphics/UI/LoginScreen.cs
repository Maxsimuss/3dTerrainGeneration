using _3dTerrainGeneration.Engine.Graphics.Backend.Shaders;
using _3dTerrainGeneration.Engine.Graphics.UI.Components;
using _3dTerrainGeneration.Engine.Graphics.UI.Screens;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace _3dTerrainGeneration.Game.Graphics.UI
{
    internal class LoginScreen : BaseScreen
    {
        Button registerButton;
        Button loginButton;
        TextField usernameField;
        TextField passwordField;
        FragmentShader shader;

        public LoginScreen()
        {
            usernameField = new TextField(textRenderer, 0, .2f, "Username");
            passwordField = new TextField(textRenderer, 0, -.2f, "Password");
            loginButton = new Button(textRenderer, 0, -.5f, usernameField.Width, .04f, new Vector4(114 / 255f, 243 / 255f, 112 / 255f, 1f), "Login");
            registerButton = new Button(textRenderer, 0, -.7f, usernameField.Width, .04f, new Vector4(248 / 255f, 101 / 255f, 101 / 255f, 1f), "Register");

            shader = new FragmentShader("Shaders/post.vert", "Shaders/loginMenu.frag");
            shader.SetInt("colortex0", 0);
            shader.SetInt("colortex1", 1);

            loginButton.Clicked += LoginButton_Clicked;
            registerButton.Clicked += RegisterButton_Clicked;
        }

        SHA512 sha = SHA512.Create();
        private void RegisterButton_Clicked()
        {
            if (usernameField.text.Length < 4 || passwordField.text.Length < 4) return;

            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(usernameField.text + ":" + passwordField.text));
            //window.network.SendPacket(new AuthenticationPacket(AuthAction.Register, hash));
        }

        private void LoginButton_Clicked()
        {
            if (usernameField.text.Length < 4 || passwordField.text.Length < 4) return;

            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(usernameField.text + ":" + passwordField.text));
            //window.network.SendPacket(new AuthenticationPacket(AuthAction.Login, hash));
        }

        public override void Render()
        {
            textRenderer.DrawTextWithShadowCentered(0, .65f, .065f, "Expidition");
            usernameField.Render();
            passwordField.Render();

            registerButton.Render();
            loginButton.Render();

        }

        public void KeyPress(char keyChar)
        {
            if (usernameField.Focused)
                usernameField.Append(keyChar);
            if (passwordField.Focused)
                passwordField.Append(keyChar);
        }

        public void BackSpacePress()
        {
            if (usernameField.Focused)
                usernameField.Remove();
            if (passwordField.Focused)
                passwordField.Remove();
        }

        public void MouseClicked(float x, float y)
        {
            usernameField.MouseClicked(x, y);
            passwordField.MouseClicked(x, y);
            loginButton.MouseClicked(x, y);
            registerButton.MouseClicked(x, y);
        }
    }
}
