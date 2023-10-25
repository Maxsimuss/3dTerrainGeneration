using OpenTK.Graphics.OpenGL;
using System;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Textures
{
    internal class Texture2D : Texture
    {
        public int Width, Height;

        public Texture2D(int width, int height, PixelInternalFormat pixelInternalFormat)
            : base(TextureTarget.Texture2D)
        {
            Width = width;
            Height = height;
            GL.TextureStorage2D(Handle, 1, (SizedInternalFormat)pixelInternalFormat, Width, Height);

            SizeInBytes = Width * Height * GetFormatBytesPerPixel(pixelInternalFormat);
            TotalBytesAllocated += SizeInBytes;
            SetFilter<Texture2D>(TextureMinFilter.Linear, TextureMagFilter.Linear);
            SetMipMap<Texture2D>(false);
            SetCompareMode<Texture2D>(TextureCompareMode.None);
            SetWrap(TextureWrapMode.ClampToEdge);
        }

        public override Texture2D SetWrap(TextureWrapMode wrapMode)
        {
            GL.TextureParameter(Handle, TextureParameterName.TextureWrapS, (int)wrapMode);
            GL.TextureParameter(Handle, TextureParameterName.TextureWrapT, (int)wrapMode);
            return this;
        }

        public void UploadData(nint data, PixelFormat pixelFormat, PixelType pixelType = PixelType.UnsignedByte)
        {
            GL.TextureSubImage2D(Handle, 0, 0, 0, Width, Height, pixelFormat, pixelType, data);
        }

        public override void UploadData<T>(T[] data, PixelFormat pixelFormat, PixelType pixelType = PixelType.UnsignedByte)
        {
            GL.TextureSubImage2D(Handle, 0, 0, 0, Width, Height, pixelFormat, pixelType, data);
        }
    }
}
