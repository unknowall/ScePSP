using LightGL;
using ScePSP.GE;
using ScePSP.Memory;
using ScePSP.Types;

namespace ScePSP.BackEnd.OpenGL
{
    public class TextureInfo
    {
        public int Width;
        public int Height;
        public TextureCacheKey TextureCacheKey;
    }

    public class TextureGL : Texture<GLBackEnd>
    {
        public GLTexture2D Texture;

        public int TextureId => (int)Texture.Texture;

        protected override void Init()
        {
            Texture = GLTexture2D.Create();
        }

        public override bool SetData(OutputPixel[] pixels, int textureWidth, int textureHeight)
        {
            Width = textureWidth;
            Height = textureHeight;

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

    public class TextureCacheGL : TextureCache<GLBackEnd, TextureGL>
    {
        public TextureCacheGL(PspMemory PspMemory, GLBackEnd BackEnd) : base(PspMemory, BackEnd)
        {
        }
    }
}