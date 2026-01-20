//#define DEBUG_TEXTURE_CACHE

using LightGL;
using ScePSP.Core;
using ScePSP.GE.State;
using ScePSP.Memory;
using ScePSP.Types;
using ScePSP.UI;
using ScePSP.Utils;
using ScePSPUtils;
using ScePSPUtils.Drawing;
using ScePSPUtils.Drawing.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace ScePSP.GE
{
    public unsafe class TextureCache : IDisposable
    {
        PspStoredConfig Config;

        public readonly Dictionary<ulong, TTexture> Cache = new Dictionary<ulong, TTexture>();

        private IntPtr _swizzlingBufferPtr;
        private readonly int _swizzlingBufferSize = 4 * 1024 * 1024;
        private IntPtr _decodedTextureBufferPtr;
        private readonly int _decodedTextureBufferSize = 1024 * 1024 * sizeof(OutputPixel);

        private PspMemory _pspMemory;
        private TTexture _invalidTexture;
        private DateTime _recheckTimestamp = DateTime.MinValue;

        public TextureCache(PspMemory pspMemory, PspStoredConfig cfg)
        {
            Config = cfg;
            _pspMemory = pspMemory;
            _swizzlingBufferPtr = Marshal.AllocHGlobal(_swizzlingBufferSize);
            _decodedTextureBufferPtr = Marshal.AllocHGlobal(_decodedTextureBufferSize);
        }

        public TTexture Get(GpuStateStruct* gpuState)
        {
            TTexture texture;

            var textureMappingState = gpuState->TextureMappingState;
            var textureState = textureMappingState.TextureState;
            var textureFormat = textureState.PixelFormat;
            uint textureAddress = textureState.Mipmap0.Address;
            int bufferWidth = textureState.Mipmap0.BufferWidth;
            int height = textureState.Mipmap0.TextureHeight;

            var textureDataSize = PixelFormatDecoder.GetPixelsSize(textureFormat, bufferWidth * height);

            byte* texturePointer = (byte*)_pspMemory.PspAddressToPointerSafe(textureAddress);

            ulong hash1 = FastHash(texturePointer, textureDataSize);

            bool recheck = false;

            if (Cache.TryGetValue(hash1, out texture))
            {
                //if (texture.Info.RecheckTimestamp != _recheckTimestamp)
                //{
                //    recheck = true;
                //}
            }
            else
            {
                recheck = true;
            }
            if (Cache.Count > Config.MaxTexCache)
            {
                var minTimeSpan = TimeSpan.FromMinutes(Config.TexMinMinutes);
                var itemsToRemove = Cache.Values.Where(item =>
                        item.Hit < Config.TexMinHits ||
                        (DateTime.UtcNow - item.Info.RecheckTimestamp) < minTimeSpan
                    ).ToList();
                foreach (var item in itemsToRemove)
                {
                    item.Dispose();
                    Cache.Remove(Cache.First(kvp => kvp.Value == item).Key);
                }
            }

            if (recheck)
            {
                texture = Decoer(gpuState);
                if (Cache.ContainsKey(hash1))
                {
                    Cache[hash1].Dispose();
                }
                Cache[hash1] = texture;
            }

            texture.Hit++;
            texture.Info.RecheckTimestamp = _recheckTimestamp;

            return texture;
        }

        public TTexture Decoer(GpuStateStruct* gpuState)
        {
            var textureMappingState = gpuState->TextureMappingState;
            var clutState = textureMappingState.ClutState;
            var textureState = textureMappingState.TextureState;

            bool swizzled = textureState.Swizzled;
            uint textureAddress = textureState.Mipmap0.Address;
            uint clutAddress = clutState.Address;
            var clutFormat = clutState.PixelFormat;
            var clutStart = clutState.Start;
            var clutCount = clutState.NumberOfColors;
            var clutShift = clutState.Shift;
            var clutMask = clutState.Mask;
            var textureFormat = textureState.PixelFormat;
            int bufferWidth = textureState.Mipmap0.BufferWidth;
            int height = textureState.Mipmap0.TextureHeight;
            var textureDataSize = PixelFormatDecoder.GetPixelsSize(textureFormat, bufferWidth * height);

            if (clutCount > 256)
            {
                clutCount = 256;
            }
            var clutDataSize = PixelFormatDecoder.GetPixelsSize(clutFormat, clutCount);

            if (!PspMemory.IsRangeValid(textureAddress, textureDataSize) || textureDataSize > 2048 * 2048 * 4)
            {
                Console.Error.WriteLineColored(ConsoleColor.DarkRed, "UPDATE_TEXTURE(TEX={0},CLUT={1}:{2}:{3}:{4}:0x{5:X},SIZE={6}x{7},{8},Swizzled={9})",
                    textureFormat, clutFormat, clutCount, clutStart, clutShift, clutMask, bufferWidth, height, bufferWidth, swizzled);
                Console.Error.WriteLineColored(ConsoleColor.DarkRed, "Invalid TEXTURE! TextureAddress=0x{0:X}, TextureDataSize={1}",
                    textureAddress, textureDataSize);

                if (_invalidTexture == null)
                {
                    _invalidTexture = new TTexture();
                    int invalidTextureWidth = 2, invalidTextureHeight = 2;
                    int invalidTextureSize = invalidTextureWidth * invalidTextureHeight;
                    _invalidTexture.PixelDataLength = invalidTextureSize * sizeof(OutputPixel);
                    _invalidTexture.PixelDataPtr = Marshal.AllocHGlobal(_invalidTexture.PixelDataLength);
                    _invalidTexture.Width = invalidTextureWidth;
                    _invalidTexture.Height = invalidTextureHeight;

                    OutputPixel* dataPtr = (OutputPixel*)_invalidTexture.PixelDataPtr;
                    var color1 = OutputPixel.FromRgba(0xFF, 0x00, 0x00, 0xFF);
                    var color2 = OutputPixel.FromRgba(0x00, 0x00, 0xFF, 0xFF);
                    for (int n = 0; n < invalidTextureSize; n++)
                    {
                        dataPtr[n] = ((n & 1) != 0) ? color1 : color2;
                    }
                }
                return _invalidTexture;
            }

            byte* texturePointer = null;
            byte* clutPointer = null;
            try
            {
                texturePointer = (byte*)_pspMemory.PspAddressToPointerSafe(textureAddress);
                clutPointer = (byte*)_pspMemory.PspAddressToPointerSafe(clutAddress);
            }
            catch (PspMemory.InvalidAddressException ex)
            {
                throw ex;
            }

            TextureInfo textureCacheKey = new TextureInfo()
            {
                TextureAddress = textureAddress,
                TextureFormat = textureFormat,
                TextureHash = FastHash(texturePointer, textureDataSize),
                ClutHash = FastHash(&(clutPointer[clutStart]), clutDataSize),
                ClutAddress = clutAddress,
                ClutFormat = clutFormat,
                ClutStart = clutStart,
                ClutShift = clutShift,
                ClutMask = clutMask,
                Swizzled = swizzled,
                ColorTestEnabled = gpuState->ColorTestState.Enabled,
                ColorTestRef = gpuState->ColorTestState.Ref,
                ColorTestMask = gpuState->ColorTestState.Mask,
                ColorTestFunction = gpuState->ColorTestState.Function
            };

            TTexture texture = new TTexture();
            texture.Info = textureCacheKey;
            texture.Width = bufferWidth;
            texture.Height = height;
            texture.Hit = 0;
            int textureWidthHeight = bufferWidth * height;

            string TextureName = "texture_" + textureCacheKey.TextureHash + "_"
                + textureCacheKey.ClutHash + "_" + textureFormat + "_"
                + clutFormat + "_" + bufferWidth + "x" + height + "_" + swizzled;

#if DEBUG_TEXTURE_CACHE
            Console.Error.WriteLine("UPDATE_TEXTURE(TEX={0},CLUT={1}:{2}:{3}:{4}:0x{5:X},SIZE={6}x{7},{8},Swizzled={9})",
            textureFormat, clutFormat, clutCount, clutStart, clutShift, clutMask, bufferWidth, height, bufferWidth, swizzled);
#endif
            OutputPixel* texturePixelsPointer = (OutputPixel*)_decodedTextureBufferPtr;
            if (swizzled)
            {
                byte* swizzlingBufferPointer = (byte*)_swizzlingBufferPtr;

                new Span<byte>(texturePointer, textureDataSize).CopyTo(new Span<byte>(swizzlingBufferPointer, textureDataSize));

                PointerUtils.Memcpy(swizzlingBufferPointer, texturePointer, textureDataSize);
                PixelFormatDecoder.UnswizzleInline(textureFormat, (void*)swizzlingBufferPointer, bufferWidth, height);
                PixelFormatDecoder.Decode(
                    textureFormat, (void*)swizzlingBufferPointer, texturePixelsPointer, bufferWidth, height,
                    clutPointer, clutFormat, clutCount, clutStart, clutShift, clutMask, strideWidth: PixelFormatDecoder.GetPixelsSize(textureFormat, bufferWidth)
                );
            }
            else
            {
                PixelFormatDecoder.Decode(
                    textureFormat, (void*)texturePointer, texturePixelsPointer, bufferWidth, height,
                    clutPointer, clutFormat, clutCount, clutStart, clutShift, clutMask, strideWidth: PixelFormatDecoder.GetPixelsSize(textureFormat, bufferWidth)
                );
            }

            if (textureCacheKey.ColorTestEnabled)
            {
                byte equalValue, notEqualValue;
                switch (textureCacheKey.ColorTestFunction)
                {
                    case ColorTestFunctionEnum.GU_ALWAYS: equalValue = 0xFF; notEqualValue = 0xFF; break;
                    case ColorTestFunctionEnum.GU_NEVER: equalValue = 0x00; notEqualValue = 0x00; break;
                    case ColorTestFunctionEnum.GU_EQUAL: equalValue = 0xFF; notEqualValue = 0x00; break;
                    case ColorTestFunctionEnum.GU_NOTEQUAL: equalValue = 0x00; notEqualValue = 0xFF; break;
                    default: throw new NotImplementedException();
                }

                for (int n = 0; n < textureWidthHeight; n++)
                {
                    if ((texturePixelsPointer[n] & textureCacheKey.ColorTestMask).Equals((textureCacheKey.ColorTestRef & textureCacheKey.ColorTestMask)))
                    {
                        texturePixelsPointer[n].A = equalValue;
                    }
                    else
                    {
                        texturePixelsPointer[n].A = notEqualValue;
                    }
                }
            }

            texture.PixelDataLength = textureWidthHeight * sizeof(OutputPixel);
            //texture.PixelDataPtr = Marshal.AllocHGlobal(texture.PixelDataLength);
            //Buffer.MemoryCopy((void*)texturePixelsPointer, (void*)texture.PixelDataPtr, texture.PixelDataLength, texture.PixelDataLength);

            if (Config.TexScaleType >= 1)
            {
                int[] Pixels = new int[textureWidthHeight];
                Marshal.Copy(_decodedTextureBufferPtr, Pixels, 0, textureWidthHeight);

                var Out = PixelsScaler.Scale(Pixels, texture.Width, texture.Height, 2, (ScaleMode)Config.TexScaleType);

                texture.Info.ScaleMode = (ScaleMode)Config.TexScaleType;
                texture.Info.ScaleX = 2;
                texture.Width = texture.Width * 2;
                texture.Height = texture.Height * 2;
                texture.PixelDataLength = Out.Length;

                texture.Initialize((byte*)Marshal.UnsafeAddrOfPinnedArrayElement(Out, 0));

                Pixels = new int[0];
                Out = new int[0];
            }
            else
            {
                texture.Initialize((byte*)texturePixelsPointer);
            }
            //texture.Save(ApplicationPaths.AssertFolder + "/" + TextureName + ".bmp");

            return texture;
        }

        public static ulong FastHash(byte* Pointer, int Count, ulong StartHash = 0)
        {
            return Utils.Hashing.FastHash(Pointer, Count, StartHash);
        }

        public void RecheckAll()
        {
            _recheckTimestamp = DateTime.UtcNow;
        }

        public void Dispose()
        {
            if (_swizzlingBufferPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_swizzlingBufferPtr);
                _swizzlingBufferPtr = IntPtr.Zero;
            }
            if (_decodedTextureBufferPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_decodedTextureBufferPtr);
                _decodedTextureBufferPtr = IntPtr.Zero;
            }
            foreach (var item in Cache.Values)
            {
                item.Dispose();
            }
            Cache.Clear();
        }
    }

    public class TextureInfo
    {
        public uint TextureAddress;
        public GuPixelFormats TextureFormat;
        public ulong TextureHash;
        public ulong ClutHash;
        public uint ClutAddress;
        public GuPixelFormats ClutFormat;
        public int ClutStart;
        public int ClutShift;
        public int ClutMask;
        public bool Swizzled;
        public bool ColorTestEnabled;
        public OutputPixel ColorTestRef;
        public OutputPixel ColorTestMask;
        public ColorTestFunctionEnum ColorTestFunction;
        public DateTime RecheckTimestamp;
        public ScaleMode ScaleMode;
        public int ScaleX;
    }

    public unsafe class TTexture : IDisposable
    {
        public TextureInfo Info;
        public int Width;
        public int Height;
        public IntPtr PixelDataPtr;
        public int PixelDataLength;
        public int Hit;
        public uint GLID;

        public void Initialize(byte* Data)
        {
            fixed (uint* TexturePtr = &GLID) GL.GenTextures(1, TexturePtr);

            GL.BindTexture(GL.GL_TEXTURE_2D, GLID);

            GL.TexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_RGBA, Width, Height, 0, GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, Data);

            //GL.TexParameteri((int)TextureTarget.Texture2d, (int)TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            //GL.TexParameteri((int)TextureTarget.Texture2d, (int)TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
            //GL.TexParameteri((int)TextureTarget.Texture2d, (int)TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            //GL.TexParameteri((int)TextureTarget.Texture2d, (int)TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }

        public void Bind()
        {
            GL.BindTexture(GL.GL_TEXTURE_2D, GLID);
        }

        public byte[] ReadPixels()
        {
            Bind();
            var Data = new byte[Width * Height * 4];
            fixed (byte* DataPtr = Data)
            {
                GL.GetTexImage(GL.GL_TEXTURE_2D, 0, GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, DataPtr);
            }
            return Data;
        }

        public void Save(string File)
        {
            var bitmap = new Bitmap(this.Width, this.Height);
            BitmapUtils.TransferChannelsDataInterleaved(
                bitmap.GetFullRectangle(),
                bitmap,
                (byte*)PixelDataPtr,
                BitmapUtils.Direction.FromDataToBitmap,
                BitmapChannel.Red,
                BitmapChannel.Green,
                BitmapChannel.Blue,
                BitmapChannel.Alpha
            );
            bitmap.Save(File);
        }

        public void Load(string FileName)
        {
            var bitmap = new Bitmap(Image.FromFile(FileName));
            OutputPixel[] data = bitmap.GetChannelsDataInterleaved(BitmapChannelList.Argb).CastToStructArray<OutputPixel>();
            PixelDataLength = data.Length;
            PixelDataPtr = Marshal.AllocHGlobal(PixelDataLength);
            var ByteData = new byte[PixelDataLength];
            data.CopyTo(ByteData, 0);
            Marshal.Copy(ByteData, 0, PixelDataPtr, data.Length);
            Width = bitmap.Width;
            Height = bitmap.Height;
            ByteData = new byte[0];
        }

        public void Dispose()
        {
            if (PixelDataPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(PixelDataPtr);
                PixelDataPtr = IntPtr.Zero;
            }

            fixed (uint* TexturePtr = &GLID) GL.DeleteTextures(1, TexturePtr);
        }
    }
}