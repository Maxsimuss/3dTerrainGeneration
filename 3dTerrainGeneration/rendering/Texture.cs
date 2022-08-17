using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace _3dTerrainGeneration.rendering
{
    public class Texture
    {
        public int Width, Height;
        public int Handle;
        private bool Mipmapped;

        public static Texture FromFile(string file)
        {
            using (var bitmap = new Bitmap(file))
            {
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Texture texture = new Texture(bitmap.Width, bitmap.Height, PixelInternalFormat.Rgba, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, false, data.Scan0);
                bitmap.UnlockBits(data);
                return texture;
            }
        }

        public Texture(int Width, int Height, PixelInternalFormat format = PixelInternalFormat.Rgb8, OpenTK.Graphics.OpenGL4.PixelFormat pixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat.Rgb, PixelType pixelType = PixelType.UnsignedByte, bool Mipmapped = false, IntPtr data = default(IntPtr), bool border = false, TextureCompareMode mode = TextureCompareMode.None, bool filtered = true)
        {
            this.Width = Width;
            this.Height = Height;
            this.Mipmapped = Mipmapped;
            Handle = GL.GenTexture();

            Use();

            GL.TexImage2D(TextureTarget.Texture2D, 0, format, Width, Height, 0, pixelFormat, pixelType, data);
            if (Mipmapped)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            }
            else
            {
                TextureMinFilter minFilter = filtered ? TextureMinFilter.Linear : TextureMinFilter.Nearest;
                TextureMagFilter magFilter = filtered ? TextureMagFilter.Linear : TextureMagFilter.Nearest;

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
            }
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)mode);
            
            if(border)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[] { 1.0f, 1.0f, 1.0f, 1.0f });
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            }
            else
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            }

            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRToTexture);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
        }

        public void Repeat()
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        }

        public void Dispose()
        {
            GL.DeleteTexture(Handle);
        }

        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
            if (Mipmapped)
            {
                GL.GenerateTextureMipmap(Handle);
            }
        }
    }
}
