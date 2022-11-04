using OpenTK.Graphics.OpenGL;
using System;
using System.Net.Http;

namespace _3dTerrainGeneration.rendering
{
    public class Texture3D<T> : Texture where T : struct
    {
        public int Width, Height, Depth;

        public Texture3D(int width, int height, int depth, PixelInternalFormat pixelInternalFormat, PixelFormat pixelFormat, PixelType pixelType = PixelType.UnsignedByte, T[] data = default)
            : base(TextureTarget.Texture3D)
        {
            Width = width;
            Height = height;
            Depth = depth;
            GL.TexImage3D(TextureTarget, 0, pixelInternalFormat, Width, Height, Depth, 0, pixelFormat, pixelType, data);
            SizeInBytes = Width * Height * Depth * GetFormatBytesPerPixel(pixelInternalFormat);
            TotalBytesAllocated += SizeInBytes;
            SetFilter<Texture3D<T>>(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
            SetMipMap<Texture3D<T>>(false);
            SetCompareMode<Texture3D<T>>(TextureCompareMode.None);
            SetWrap(TextureWrapMode.ClampToBorder);
        }

        public override Texture3D<T> SetWrap(TextureWrapMode wrapMode)
        {
            GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapS, (int)wrapMode);
            GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapT, (int)wrapMode);
            GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapR, (int)wrapMode);
            return this;
        }
    }
}
