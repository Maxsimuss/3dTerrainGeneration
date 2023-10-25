using OpenTK.Graphics.OpenGL;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Textures
{
    internal class Texture3D : Texture
    {
        public int Width, Height, Depth;

        public Texture3D(int width, int height, int depth, PixelInternalFormat pixelInternalFormat)
            : base(TextureTarget.Texture3D)
        {
            Width = width;
            Height = height;
            Depth = depth;
            GL.TextureStorage3D(Handle, 1, (SizedInternalFormat)pixelInternalFormat, Width, Height, Depth);
            SizeInBytes = Width * Height * Depth * GetFormatBytesPerPixel(pixelInternalFormat);
            TotalBytesAllocated += SizeInBytes;
            SetFilter<Texture3D>(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
            SetMipMap<Texture3D>(false);
            SetCompareMode<Texture3D>(TextureCompareMode.None);
            SetWrap(TextureWrapMode.ClampToBorder);
        }

        public override Texture3D SetWrap(TextureWrapMode wrapMode)
        {
            GL.TextureParameter(Handle, TextureParameterName.TextureWrapS, (int)wrapMode);
            GL.TextureParameter(Handle, TextureParameterName.TextureWrapT, (int)wrapMode);
            GL.TextureParameter(Handle, TextureParameterName.TextureWrapR, (int)wrapMode);
            return this;
        }

        public override void UploadData<T>(T[] data, PixelFormat pixelFormat, PixelType pixelType = PixelType.UnsignedByte)
        {
            GL.TextureSubImage3D(Handle, 0, 0, 0, 0, Width, Height, Depth, pixelFormat, pixelType, data);
        }
    }
}
