using LightGL;
using ScePSP.Core.Memory;
using ScePSP.Core.Types;

namespace ScePSP.Core.GpuBackEnd.OpenGL
{
    public class TextureInfo
    {
        public int Width;
        public int Height;
        public OutputPixel[] Data;
        public TextureCacheKey TextureCacheKey;
    }

    public class TextureGL : PspTexture
    {
        public GLTexture2D Texture;

        public int TextureId => (int)Texture.Texture;

        public override void Init()
        {
            Texture = GLTexture2D.Create();
        }

        public override bool SetData(OutputPixel[] pixels, int textureWidth, int textureHeight)
        {
            Width = textureWidth;
            Height = textureHeight;
            Data = pixels;

            Bind();

            Texture.SetFormat(TextureFormat.RGBA).SetSize(textureWidth, textureHeight).SetData(pixels);

            return true;
        }

        public override void Bind()
        {
            Texture.Bind();
        }

        public override void Dispose()
        {
            if (TextureId != 0)
            {
                Texture.Dispose();
            }
        }
    }

    public class TextureCacheGL : TextureCache<TextureGL>
    {
        public TextureCacheGL(PspMemory pspMemory) : base(pspMemory)
        {
        }
    }
}