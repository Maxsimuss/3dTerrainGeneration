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
            GL.TextureStorage1D(Handle, 1, (SizedInternalFormat)pixelInternalFormat, Width);

            SizeInBytes = Width * GetFormatBytesPerPixel(pixelInternalFormat);
            TotalBytesAllocated += SizeInBytes;

            SetFilter<Texture1D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
            SetMipMap<Texture1D>(false);
            SetCompareMode<Texture1D>(TextureCompareMode.None);
            SetWrap(TextureWrapMode.ClampToEdge);
        }

        public override Texture1D SetWrap(TextureWrapMode wrapMode)
        {
            GL.TextureParameter(Handle, TextureParameterName.TextureWrapS, (int)wrapMode);
            return this;
        }

        public override void UploadData<T>(T[] data, PixelFormat pixelFormat, PixelType pixelType = PixelType.UnsignedByte)
        {
            GL.TextureSubImage1D(Handle, 0, 0, Width, pixelFormat, pixelType, data);
            throw new NotImplementedException();
        }
    }
}
