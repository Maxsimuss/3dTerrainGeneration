using OpenTK.Graphics.OpenGL;
using System;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Textures
{
    internal class Texture2D : Texture
    {
        public int Width, Height;

        public Texture2D(int width, int height, PixelInternalFormat pixelInternalFormat, PixelFormat pixelFormat, PixelType pixelType = PixelType.UnsignedByte, IntPtr data = default)
            : base(TextureTarget.Texture2D)
        {
            Width = width;
            Height = height;
            GL.TextureStorage2D(Handle, 1, (SizedInternalFormat)pixelInternalFormat, Width, Height);
            
            if(data != default)
                GL.TextureSubImage2D(Handle, 0, 0, 0, width, height, pixelFormat, pixelType, data);
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
    }
}
