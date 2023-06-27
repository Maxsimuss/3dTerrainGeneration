using OpenTK.Graphics.OpenGL;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Textures
{
    internal class Texture3D<T> : Texture where T : struct
    {
        public int Width, Height, Depth;

        public Texture3D(int width, int height, int depth, PixelInternalFormat pixelInternalFormat, PixelFormat pixelFormat, PixelType pixelType = PixelType.UnsignedByte, T[] data = default)
            : base(TextureTarget.Texture3D)
        {
            Width = width;
            Height = height;
            Depth = depth;
            GL.TextureStorage3D(Handle, 1, (SizedInternalFormat)pixelInternalFormat, Width, Height, Depth);
            GL.TextureSubImage3D(Handle, 0, 0, 0, 0, width, height, depth, pixelFormat, pixelType, data);
            SizeInBytes = Width * Height * Depth * GetFormatBytesPerPixel(pixelInternalFormat);
            TotalBytesAllocated += SizeInBytes;
            SetFilter<Texture3D<T>>(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
            SetMipMap<Texture3D<T>>(false);
            SetCompareMode<Texture3D<T>>(TextureCompareMode.None);
            SetWrap(TextureWrapMode.ClampToBorder);
        }

        public override Texture3D<T> SetWrap(TextureWrapMode wrapMode)
        {
            GL.TextureParameter(Handle, TextureParameterName.TextureWrapS, (int)wrapMode);
            GL.TextureParameter(Handle, TextureParameterName.TextureWrapT, (int)wrapMode);
            GL.TextureParameter(Handle, TextureParameterName.TextureWrapR, (int)wrapMode);
            return this;
        }
    }
}
