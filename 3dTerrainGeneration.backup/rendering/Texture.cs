using OpenTK.Graphics.OpenGL4;
using System;

namespace _3dTerrainGeneration.rendering
{
    class Texture
    {
        public int Handle;

        public Texture(int Width, int Height, PixelInternalFormat format = PixelInternalFormat.Rgba8, PixelFormat pixelFormat = PixelFormat.Rgb, string path = null)
        {
            Handle = GL.GenTexture();

            Use();

            GL.TexImage2D(TextureTarget.Texture2D, 0, format, Width, Height, 0, pixelFormat, PixelType.UnsignedByte, (IntPtr)0);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRToTexture);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
        }

        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }
    }
}
