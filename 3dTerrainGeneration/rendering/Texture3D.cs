using OpenTK.Graphics.OpenGL;
using System;

namespace _3dTerrainGeneration.rendering
{
    public class Texture3D : Texture
    {
        public int Width, Height, Depth;

        public Texture3D(int width, int height, int depth, PixelInternalFormat pixelInternalFormat, PixelFormat pixelFormat, PixelType pixelType = PixelType.UnsignedByte, IntPtr data = default)
            : base(TextureTarget.Texture3D)
        {
            Width = width;
            Height = height;
            Depth = depth;
            GL.TexImage3D(TextureTarget, 0, pixelInternalFormat, Width, Height, Depth, 0, pixelFormat, pixelType, data);
            SizeInBytes = Width * Height * Depth * GetFormatBytesPerPixel(pixelInternalFormat);
            TotalBytesAllocated += SizeInBytes;
            SetFilter<Texture3D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
            SetMipMap<Texture3D>(false);
            SetCompareMode<Texture3D>(TextureCompareMode.None);
            SetWrap(TextureWrapMode.ClampToBorder);
        }

        public override Texture3D SetWrap(TextureWrapMode wrapMode)
        {
            GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapS, (int)wrapMode);
            GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapT, (int)wrapMode);
            GL.TexParameter(TextureTarget, TextureParameterName.TextureWrapR, (int)wrapMode);
            return this;
        }
    }
}
