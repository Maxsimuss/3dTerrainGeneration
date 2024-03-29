﻿using _3dTerrainGeneration.Engine.Graphics.Backend.Shaders;
using _3dTerrainGeneration.Engine.Graphics.Backend.Textures;
using _3dTerrainGeneration.Engine.Util;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Numerics;

namespace _3dTerrainGeneration.Engine.Graphics.UI.Text
{
    public class TextRenderer
    {
        private static readonly Vector4 shadowColor = new Vector4(0, 0, 0, 1);

        private static TextRenderer instance;
        public static TextRenderer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TextRenderer();
                }

                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        private FragmentShader TextShader;
        private Texture2D texture;

        private int GlyphSize;

        private int VAO, VBO;

        private TextRenderer()
        {
            string fontPath = ResourceManager.GetFontPath("PressStart2P-Regular.ttf");

            var collection = new PrivateFontCollection();
            collection.AddFontFile(fontPath);
            Font font = new Font(collection.Families[0], 8);
            font = new Font(collection.Families[0], 8 * (8f / font.Height));

            GlyphSize = font.Height + 2;

            int bitmapWidth = 256 * GlyphSize;

            using (Bitmap bitmap = new Bitmap(bitmapWidth, GlyphSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.None;
                    g.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;

                    for (int i = 0; i < 256; i++)
                    {
                        char c = (char)i;
                        g.DrawString(c.ToString(), font, Brushes.White, i * GlyphSize, 0);
                    }
                }

                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                texture = new Texture2D(bitmap.Width, bitmap.Height, PixelInternalFormat.Rgba8, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0).SetFilter<Texture2D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
                bitmap.UnlockBits(data);
            }
            TextShader = new FragmentShader("2D/Textured.vert", "2D/Textured.frag");
            TextShader.Compile();
            TextShader.SetInt("colortex0", 0);
            texture.ActiveBind();

            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();

            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        }

        public void DrawTextWithShadowCentered(float x, float y, float scale, string text)
        {
            DrawTextWithShadowCentered(x, y, scale, text, Vector4.One);
        }

        public void DrawTextWithShadowCentered(float x, float y, float scale, string text, Vector4 color)
        {
            x -= scale * text.Length / 2;
            y -= scale * GraphicsEngine.Instance.AspectRatio / 2;
            DrawTextWithShadow(x, y, scale, text, color);
        }

        public void DrawTextWithShadow(float x, float y, float scale, string text)
        {
            DrawTextWithShadow(x, y, scale, text, Vector4.One);
        }

        public void DrawTextWithShadow(float x, float y, float scale, string text, Vector4 color)
        {
            DrawText(x + scale * .05f, y + scale * .05f, scale, text, shadowColor);
            DrawText(x, y, scale, text, color);
        }

        public void DrawText(float x, float y, float scale, string text)
        {
            DrawText(x, y, scale, text, Vector4.One);
        }

        public void DrawTextCentered(float x, float y, float scale, string text)
        {
            DrawTextCentered(x, y, scale, text, Vector4.One);
        }

        public void DrawTextCentered(float x, float y, float scale, string text, Vector4 color)
        {
            x -= scale * text.Length / 2;
            y -= scale * GraphicsEngine.Instance.AspectRatio / 2;
            DrawText(x, y, scale, text, color);
        }

        public void DrawText(float x, float y, float scale, string text, Vector4 color)
        {
            float scaleY = scale * GraphicsEngine.Instance.AspectRatio;

            float[] buffer = new float[text.Length * 24];

            float u_step = 1f / 256f;

            int offset = 0;
            float uvTop = (float)(GlyphSize - 2) / GlyphSize;
            float u_stepReal = 1f / 256f * uvTop;

            for (int n = 0; n < text.Length; n++)
            {
                char idx = text[n];
                float u = idx % 256 * u_step;

                vertex2(ref offset, x, y);
                vertex2(ref offset, u, uvTop);

                vertex2(ref offset, x + scale, y);
                vertex2(ref offset, u + u_stepReal, uvTop);

                vertex2(ref offset, x + scale, y + scaleY);
                vertex2(ref offset, u + u_stepReal, 0);

                vertex2(ref offset, x, y);
                vertex2(ref offset, u, uvTop);

                vertex2(ref offset, x + scale, y + scaleY);
                vertex2(ref offset, u + u_stepReal, 0);

                vertex2(ref offset, x, y + scaleY);
                vertex2(ref offset, u, 0);

                x += scale;
            }


            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * buffer.Length, buffer, BufferUsageHint.DynamicDraw);
            TextShader.Use();
            TextShader.SetVector4("color", color);
            texture.ActiveBind();
            GL.DrawArrays(PrimitiveType.Triangles, 0, buffer.Length / 4);

            void vertex2(ref int offset, float x, float y)
            {
                buffer[offset++] = x;
                buffer[offset++] = y;
            }
        }
    }
}
