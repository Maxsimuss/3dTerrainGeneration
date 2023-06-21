using OpenTK.Graphics.OpenGL;
using System;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Textures
{
    internal class Texture1D : Texture
    {
        public int Width;

        public Texture1D(int width, PixelInternalFormat pixelInternalFormat, PixelFormat pixelFormat, PixelType pixelType = PixelType.UnsignedByte, IntPtr data = default)
            : base(TextureTarget.Texture1D)
        {
            Width = width;
            GL.TexImage1D(TextureTarget, 0, pixelInternalFormat, Width, 0, pixelFormat, pixelType, data);
            SizeInBytes = Width * GetFormatBytesPerPixel(pixelInternalFormat);
            TotalBytesAllocated += SizeInBytes;

            SetFilter<Texture1D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
            SetMipMap<Texture1D>(false);
            SetCompareMode<Texture1D>(TextureCompareMode.None);
            SetWrap(TextureWrapMode.ClampToEdge);
        }

        public override Texture1D SetWrap(TextureWrapMode wrapMode)
        {
            GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapS, (int)wrapMode);
            return this;
        }
    }
}
