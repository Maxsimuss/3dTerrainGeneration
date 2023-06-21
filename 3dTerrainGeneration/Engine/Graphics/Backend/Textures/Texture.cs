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
                case PixelInternalFormat.R8: return 1;
                case PixelInternalFormat.R16f: return 2;
                case PixelInternalFormat.R32f: return 4;
                case PixelInternalFormat.R8ui: return 1;
                case (PixelInternalFormat)All.Rgb565: return 2;
            }

            throw new NotImplementedException();
        }

        public int Handle;
        private bool Mipmapped;
        public int SizeInBytes = 0;
        public TextureTarget TextureTarget { get; private set; }

        protected Texture(TextureTarget textureTarget)
        {
            TextureTarget = textureTarget;
            Handle = GL.GenTexture();
            Bind();
        }


        public abstract Texture SetWrap(TextureWrapMode wrapMode);

        public T SetFilter<T>(TextureMinFilter minFilter, TextureMagFilter magFilter) where T : Texture
        {
            GL.TexParameter(TextureTarget, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget, TextureParameterName.TextureMagFilter, (int)magFilter);

            return (T)this;
        }

        public T SetMipMap<T>(bool mipmap) where T : Texture
        {
            Mipmapped = mipmap;
            return (T)this;
        }

        public T SetBorderColor<T>(float r, float g, float b, float a) where T : Texture
        {
            GL.TexParameter(TextureTarget, TextureParameterName.TextureBorderColor, new float[] { r, g, b, a });
            return (T)this;
        }

        public T SetCompareMode<T>(TextureCompareMode compareMode) where T : Texture
        {
            GL.TexParameter(TextureTarget, TextureParameterName.TextureCompareMode, (int)compareMode);
            return (T)this;
        }

        public void Delete()
        {
            GL.DeleteTexture(Handle);
            TotalBytesAllocated -= SizeInBytes;
            SizeInBytes = 0;
        }

        public virtual void ActiveBind(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget, Handle);
        }

        public void Bind()
        {
            GL.BindTexture(TextureTarget, Handle);
        }

        public void UnBind()
        {
            GL.BindTexture(TextureTarget, 0);
        }

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
