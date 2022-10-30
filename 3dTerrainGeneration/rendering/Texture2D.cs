using OpenTK.Graphics.OpenGL;
using System;

namespace _3dTerrainGeneration.rendering
{
    public class Texture2D : Texture
    {
        public int Width, Height;

        public Texture2D(int width, int height, PixelInternalFormat pixelInternalFormat, PixelFormat pixelFormat, PixelType pixelType = PixelType.UnsignedByte, IntPtr data = default)
            : base(TextureTarget.Texture2D)
        {
            Width = width;
            Height = height;
            GL.TexImage2D(TextureTarget, 0, pixelInternalFormat, Width, Height, 0, pixelFormat, pixelType, data);
            SizeInBytes = Width * Height * GetFormatBytesPerPixel(pixelInternalFormat);
            TotalBytesAllocated += SizeInBytes;
            SetFilter<Texture2D>(TextureMinFilter.Linear, TextureMagFilter.Linear);
            SetMipMap<Texture2D>(false);
            SetCompareMode<Texture2D>(TextureCompareMode.None);
            SetWrap(TextureWrapMode.ClampToEdge);
        }

        public override Texture2D SetWrap(TextureWrapMode wrapMode)
        {
            GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapS, (int)wrapMode);
            GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapT, (int)wrapMode);
            return this;
        }
    }
}
