using _3dTerrainGeneration.rendering;
using _3dTerrainGeneration.util;
using OpenTK.Graphics.OpenGL;
using System;
using System.Numerics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TerrainServer.network.packet;

namespace _3dTerrainGeneration.gui
{
    internal class LoginScreen
    {
        FontRenderer renderer;
        GuiButton registerButton;
        GuiButton loginButton;
        GuiTextField usernameField;
        GuiTextField passwordField;
        Window window;
        Camera camera;
        FragmentShader shader;

        public LoginScreen(FontRenderer renderer, Window window)
        {
            this.renderer = renderer;
            this.window = window;
            usernameField = new GuiTextField(renderer, 0, .2f, "Username");
            passwordField = new GuiTextField(renderer, 0, -.2f, "Password");
            loginButton = new GuiButton(renderer, 0, -.5f, usernameField.width, .04f, new Vector4(114 / 255f, 243 / 255f, 112 / 255f, 1f), "Login");
            registerButton = new GuiButton(renderer, 0, -.7f, usernameField.width, .04f, new Vector4(248 / 255f, 101 / 255f, 101 / 255f, 1f), "Register");
            camera = new Camera(Vector3.Zero, renderer.aspectRatio);
            camera.Fov = 70;
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
            window.network.SendPacket(new AuthenticationPacket(AuthAction.Register, hash));
        }

        private void LoginButton_Clicked()
        {
            if (usernameField.text.Length < 4 || passwordField.text.Length < 4) return;

            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(usernameField.text + ":" + passwordField.text));
            window.network.SendPacket(new AuthenticationPacket(AuthAction.Login, hash));
        }

        public void Render()
        {
            camera.AspectRatio = renderer.aspectRatio;
            camera.Yaw = (float)(TimeUtil.Unix() / 360 % 360);
            camera.Pitch = (float)OpenTK.MathHelper.RadiansToDegrees(Math.Sin(TimeUtil.Unix() / 36000 % Math.PI * 2)) / 2;

            double t = TimeUtil.Unix() / 100 / 1440 % 1;

            double X = Math.Cos(t * 2 * Math.PI - Math.PI * .5) * Math.Cos(25);
            double Y = Math.Sin(t * 2 * Math.PI - Math.PI * .5) * Math.Cos(25);
            double Z = Math.Sin(25);

            Vector3 sunPos = new Vector3((float)X, (float)Y, (float)Z);

            Matrix4x4 projInv = camera.GetProjectionMatrix();
            Matrix4x4 view = camera.GetViewMatrix();
            Matrix4x4.Invert(projInv, out projInv);

            GL.Viewport(0, 0, window.SkyBuffer.Width, window.SkyBuffer.Height);
            window.Sky.SetMatrix4("projection", projInv);
            window.Sky.SetMatrix4("viewMatrix", view);
            window.Sky.SetVector3("sun_dir", sunPos);
            window.Sky.SetFloat("time", (float)(TimeUtil.Unix() / 5000D % 3600));
            FragmentPass.BeginPostStage();
            FragmentPass.Apply(window.Sky, window.SkyBuffer);

            GL.Viewport(0, 0, window.StarBuffer.Width, window.StarBuffer.Height);
            window.Stars.SetMatrix4("projection", projInv);
            window.Stars.SetMatrix4("viewMatrix", view);
            window.Stars.SetVector3("sun_dir", sunPos);
            window.Stars.SetFloat("time", (float)(TimeUtil.Unix() / 5000D % 3600));
            FragmentPass.Apply(window.Stars, window.StarBuffer);
            FragmentPass.Apply(shader, null, window.StarBuffer.colorTex[0], window.SkyBuffer.colorTex[0]);



            renderer.DrawTextWithShadowCentered(0, .65f, .065f, "Expidition");
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
            if(usernameField.Focused)
                usernameField.Remove();
            if(passwordField.Focused)
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
