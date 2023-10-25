using OpenTK.Graphics.OpenGL;
using System;

namespace _3dTerrainGeneration.Engine.Graphics.Backend.Textures
{
    internal abstract class Texture
    {
        public static long TotalBytesAllocated = 0;
        protected static int GetFormatBytesPerPixel(PixelInternalFormat pixelInternalFormat)
        {
            switch (pixelInternalFormat)
            {
                case PixelInternalFormat.Rgba8: return 4;
                case PixelInternalFormat.Rgba16f: return 8;
                case PixelInternalFormat.Rgba32f: return 16;
                case PixelInternalFormat.Rgb8: return 3;
                case PixelInternalFormat.R11fG11fB10f: return 4;
                case PixelInternalFormat.Rgb16f: return 6;
                case PixelInternalFormat.Rgb32f: return 12;
                case PixelInternalFormat.DepthComponent32f: return 4;
                case PixelInternalFormat.DepthComponent24: return 3;
                case PixelInternalFormat.DepthComponent16: return 2;
                case PixelInternalFormat.R8: return 1;
                case PixelInternalFormat.R3G3B2: return 1;
                case PixelInternalFormat.R16f: return 2;
                case PixelInternalFormat.R32f: return 4;
                case PixelInternalFormat.R8ui: return 1;
                case (PixelInternalFormat)All.Rgb565: return 2;
            }

            throw new NotImplementedException();
        }

        public int Handle;
        public int SizeInBytes = 0;
        private bool Mipmapped;

        protected Texture()
        {
        }

        protected Texture(TextureTarget textureTarget)
        {
            int[] textures = new int[1];
            GL.CreateTextures(textureTarget, 1, textures);
            Handle = textures[0];
        }

        public abstract Texture SetWrap(TextureWrapMode wrapMode);

        public T SetFilter<T>(TextureMinFilter minFilter, TextureMagFilter magFilter) where T : Texture
        {
            GL.TextureParameter(Handle, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TextureParameter(Handle, TextureParameterName.TextureMagFilter, (int)magFilter);

            return (T)this;
        }

        public T SetMipMap<T>(bool mipmap) where T : Texture
        {
            Mipmapped = mipmap;
            return (T)this;
        }

        public T SetBorderColor<T>(float r, float g, float b, float a) where T : Texture
        {
            GL.TextureParameter(Handle, TextureParameterName.TextureBorderColor, new float[] { r, g, b, a });
            return (T)this;
        }

        public T SetCompareMode<T>(TextureCompareMode compareMode) where T : Texture
        {
            GL.TextureParameter(Handle, TextureParameterName.TextureCompareMode, (int)compareMode);
            return (T)this;
        }

        public void Delete()
        {
            GL.DeleteTexture(Handle);
            TotalBytesAllocated -= SizeInBytes;
            SizeInBytes = 0;
        }

        public virtual void ActiveBind(int unit = 0)
        {
            GL.BindTextureUnit(unit, Handle);
        }

        public abstract void UploadData<T>(T[] data, PixelFormat pixelFormat, PixelType pixelType = PixelType.UnsignedByte) where T : struct;

        public Texture GenerateMipMap()
        {
            if (Mipmapped)
            {
                GL.GenerateTextureMipmap(Handle);
            }
            else
            {
                throw new InvalidOperationException("Cannot generate mipmaps for a non mipmapped texture!");
            }

            return this;
        }
    }
}
