using LightCodec;
using LightCodec.atrac3;
using LightCodec.atrac3plus;
using ScePSP.Hle.Formats;
using ScePSP.Hle.Formats.audio;
using ScePSP.Hle.Managers;
using ScePSPUtils.Endian;
using ScePSPUtils.Extensions;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ScePSP.Hle.Modules.libatrac3plus
{
    public unsafe partial class sceAtrac3plus : HleModuleHost
    {
        public enum CodecType
        {
            PSP_MODE_AT_3_PLUS = 0x00001000,
            PSP_MODE_AT_3 = 0x00001001,
        }

        public struct OMAHeader
        {
            public UintBe Magic;
            public UshortBe StructSize;
            public UshortBe Unknown0;
            public UintBe Unknown1;
            public UintBe Unknown2;
            public UintBe Unknown3;
            public UintBe Unknown4;
            public UintBe Unknown5;
            public UintBe Unknown6;

            // Must set from AT3.
            public uint OmaInfo;

            private fixed byte Pad[60];

            public OMAHeader(uint omaInfo)
            {
                this.Magic = 0x45413301;
                this.StructSize = (ushort)sizeof(OMAHeader);
                this.Unknown0 = unchecked((ushort)-1);
                this.Unknown1 = 0x00000000;
                this.Unknown2 = 0x010f5000;
                this.Unknown3 = 0x00040000;
                this.Unknown4 = 0x0000f5ce;
                this.Unknown5 = 0xd2929132;
                this.Unknown6 = 0x2480451c;
                this.OmaInfo = omaInfo;
            }
        }

        [HleUidPoolClass(NotFoundError = SceKernelErrors.ERROR_ATRAC_BAD_ID, FirstItem = 0, ReuseIds = true)]
        public class Atrac : IHleUidPoolClass
        {
            protected HleMemoryManager HleMemoryManager => PSPDrivers.HLE.MemoryManager;

            public At3FormatStruct Format;
            public FactStruct Fact;
            public SmplStruct Smpl;
            public LoopInfoStruct[] LoopInfoList;

            int BlockSize;

            public int startSkippedSamples;

            public CodecType CodecType;

            public int NumberOfLoops;

            public int CurrentFrame;

            public int MaximumSamples
            {
                get
                {
                    switch (CodecType)
                    {
                        case sceAtrac3plus.CodecType.PSP_MODE_AT_3_PLUS:
                            startSkippedSamples = 368;
                            return Atrac3plusData2.ATRAC3P_FRAME_SAMPLES;
                        case sceAtrac3plus.CodecType.PSP_MODE_AT_3:
                            startSkippedSamples = 69;
                            return Atrac3Decoder.SAMPLES_PER_FRAME;
                        default:
                            startSkippedSamples = 0;
                            return 0;
                    }
                }
            }

            public int DecodingOffset
            {
                get => CurrentFrame * BlockSize;
                set
                {
                    CurrentFrame = value & ~0x7FF;
                    if (MaximumSamples == 0)
                    {
                        InputBuffer.Position = HeadSize;
                    }
                    else
                    {
                        InputBuffer.Position = CurrentFrame * BlockSize + HeadSize;
                    }
                }
            }

            protected internal const int PSP_ATRAC_STATUS_NONLOOP_STREAM_DATA = 0;
            protected internal const int PSP_ATRAC_STATUS_LOOP_STREAM_DATA = 1;
            public int LoopStatus
            {
                get
                {
                    if (NumberOfLoops > 0)
                    {
                        return PSP_ATRAC_STATUS_NONLOOP_STREAM_DATA;
                    }
                    return PSP_ATRAC_STATUS_LOOP_STREAM_DATA;
                }
            }

            public int HeadSize;

            public int CurrentEndSample;

            public int TotalSample;

            public int EndSample => Fact.EndSample;

            public bool DecodingReachedEnd => RemainingFrames <= 0;

            public int RemainingFrames
            {
                get
                {
                    if (BlockSize == 0) return -1;
                    return (int)(InputBuffer.Available() / BlockSize);
                }
            }

            short[] at3OutData = new short[16384];

            public PspBuffer InputBuffer;

            public ILightCodec Decoder;

            public MemoryPartition PrimaryBuffer;

            public bool SecondBufferNeeded;

            public bool SecondBufferSet;

            public uint SecondBufferPosition;

            public uint SecondBufferSize;

            public enum CompressionCode : ushort
            {
                Unknown = 0x0000,
                PcmUncompressed = 0x0001,
                MicrosoftAdpcm = 0x0002,
                ItuG711ALaw = 0x0006,
                ItuG711AmLaw = 0x0007,
                ImaAdpcm = 0x0011,
                ItuG723AdpcmYamaha = 0x0016,
                Gsm610 = 0x0031,
                ItuG721Adpcm = 0x0040,
                Mpeg = 0x0050,
                Atrac3 = 0x0270,
                Atrac3Plus = 0xFFFE,
                Experimental = 0xFFFF,
            }

            public struct WavFormatStruct
            {
                public CompressionCode CompressionCode;

                public ushort AtracChannels;

                public int Bitrate;

                public uint BytesPerSecond;

                public ushort BlockAlignment;
            }

            [StructLayout(LayoutKind.Explicit)]
            public struct At3FormatStruct
            {
                [FieldOffset(0x0000)] public CompressionCode CompressionCode;

                [FieldOffset(0x0002)] public ushort AtracChannels;

                [FieldOffset(0x0004)] public uint Bitrate;

                [FieldOffset(0x0008)] public uint AverageBytesPerSecond;

                [FieldOffset(0x000A)] public ushort BlockAlignment;

                [FieldOffset(0x000C)] public ushort BytesPerFrame;

                [FieldOffset(0x0010)] private fixed uint Unknown[6];

                [FieldOffset(0x0028)] public uint OmaInfo;

                [FieldOffset(0x0028)] private UshortBe _Unk2;

                [FieldOffset(0x002A)] private UshortBe _BlockSize;

                public int BlockSize => (_BlockSize & 0x3FF) * 8 + 8;
            }

            public struct FactStruct
            {
                public int EndSample;

                public int SampleOffset;
            }

            public struct SmplStruct
            {
                private fixed uint Unknown[7];

                public uint LoopCount;

                private fixed uint Unknown2[1];
            }

            public struct LoopInfoStruct
            {
                public uint CuePointID;

                public uint Type;

                public int StartSample;

                public int EndSample;

                public uint Fraction;

                public int PlayCount;
            }

            public Atrac(CodecType CodecType)
            {
                PrimaryBuffer = HleMemoryManager.GetPartition(MemoryPartitions.User).Allocate(1024 * 1024);

                this.CodecType = CodecType;
            }

            public Atrac(byte* Data, int DataLength)
            {
                PrimaryBuffer = HleMemoryManager.GetPartition(MemoryPartitions.User).Allocate(1024 * 1024);

                CodecType = CodecType.PSP_MODE_AT_3_PLUS;

                SetData(Data, DataLength);
            }

            public Atrac(byte* Data, int DataLength, int ReadSize, bool IsMoon = false)
            {
                PrimaryBuffer = HleMemoryManager.GetPartition(MemoryPartitions.User).Allocate(1024 * 1024);

                CodecType = CodecType.PSP_MODE_AT_3_PLUS;

                SetData(Data, DataLength, ReadSize, IsMoon);
            }

            public void SetData(byte* Data, int DataLength)
            {
                ParseAtracData(new UnmanagedMemoryStream(Data, DataLength), Data, DataLength);
            }

            public void SetData(byte* Data, int DataLength, int ReadSize, bool IsMoon = false)
            {
                ParseAtracData(new UnmanagedMemoryStream(Data, DataLength), Data, DataLength);
            }

            private void ParseAtracData(Stream Stream, byte* Data, int DataLength)
            {
                var RiffWaveReader = new RiffWaveReader();

                RiffWaveReader.HandleChunk += (ChunkType, ChunkSize, ChunkStream) =>
                {
                    switch (ChunkType)
                    {
                        case "fmt ":
                            Format = ChunkStream.ReadStructPartiallyEx<At3FormatStruct>();
                            BlockSize = Format.BytesPerFrame;
                            break;
                        case "fact":
                            Fact = ChunkStream.ReadStructPartiallyEx<FactStruct>();
                            break;
                        case "smpl":
                            Smpl = ChunkStream.ReadStructPartiallyEx<SmplStruct>();
                            LoopInfoList = ChunkStream.ReadStructVectorEx<LoopInfoStruct>(Smpl.LoopCount);

                            //Console.WriteLine("AT3 smpl: {0}", Smpl.LoopCount);
                            for(int i = 0; i< LoopInfoList.Length; i++)
                            {
                                var LoopInfo = LoopInfoList[i];

                                LoopInfo.StartSample -= Fact.SampleOffset;
                                LoopInfo.EndSample -= Fact.SampleOffset;

                                //Console.WriteLine($"Atrac Loop[{i}]: StartSample {LoopInfo.StartSample} EndSample {LoopInfo.EndSample} " +
                                //    $"PlayCount {LoopInfo.PlayCount} Type {LoopInfo.Type} Fraction {LoopInfo.Fraction}");
                            }

                            break;
                        case "data":
                            HeadSize = RiffWaveReader.HeadSize;
                            InputBuffer = new PspBuffer(Data, (int)(ChunkSize + HeadSize), DataLength, 0);
                            InputBuffer.notifyRead(HeadSize);
                            TotalSample = (InputBuffer.MaxSize - HeadSize) / Format.BytesPerFrame;
                            break;
                        default:
                            throw new NotImplementedException($"Can't handle chunk '{ChunkType}'");
                    }
                };
                RiffWaveReader.ParseFile(Stream);

                switch (Format.CompressionCode)
                {
                    case CompressionCode.Atrac3:
                        Decoder = CodecFactory.Get(AudioCodec.AT3);
                        break;
                    case CompressionCode.Atrac3Plus:
                        Decoder = CodecFactory.Get(AudioCodec.AT3plus);
                        break;
                    default:
                        Decoder = CodecFactory.Get(AudioCodec.NULL);
                        Console.WriteLine($"sceAtrac3Plus no sopport codec: {Format.CompressionCode}");
                        return;
                }

                Console.WriteLine($"Atrac Format: {Format.CompressionCode} DataSize 0x{InputBuffer.CurrentSize:X} BlockSize {Format.BytesPerFrame}");

                Decoder.init(Format.BytesPerFrame, Format.AtracChannels, Format.AtracChannels, 0);

                CurrentFrame = 0;
            }

            public void SetSecondBuffer(byte* SecondBufferAddr, uint uiSecondBufferByte)
            {
                InputBuffer.Write(SecondBufferAddr, (int)uiSecondBufferByte);

                SecondBufferSize -= uiSecondBufferByte;

                //Console.WriteLine($"SetSecondBuffer 0x{InputBuffer.Available():X} / 0x{InputBuffer.CurrentSize:X}");
            }

            public void AddStreamData(int bytesToAdd)
            {
                InputBuffer.Write((byte*)PrimaryBuffer.LowPointer, bytesToAdd);

                //Console.WriteLine($"AddStreamData 0x{InputBuffer.Available():X} / 0x{InputBuffer.CurrentSize:X}");
            }

            public int PositionFromSample(int sample)
            {
                return BlockSize * sample + HeadSize;
            }

            public void SetPlayPosition(int uiSample, int uiWriteByteFirstBuf, int uiWriteByteSecondBuf)
            {
                //Console.WriteLine($"SetPlayPosition {uiSample} 0x{uiWriteByteFirstBuf:X}");

                if (uiSample != CurrentFrame && uiSample >= 0 && uiSample < TotalSample)
                {
                    if (InputBuffer.CurrentSize != InputBuffer.MaxSize)
                    {
                        InputBuffer.notifyWrite(uiWriteByteFirstBuf);
                    }

                    InputBuffer.Position = PositionFromSample(uiSample);

                    CurrentFrame = uiSample;
                }
            }

            public int Decode(StereoShortSoundSample* SamplesOut)
            {
                if (SamplesOut == null) return 0;

                //int BlockSize = Format.BlockSize;

                if (BlockSize <= 0)
                {
                    Console.WriteLine("BlockSize <= 0");
                    return -1;
                }

                if (InputBuffer.Available() < BlockSize)
                {
                    Console.WriteLine("EndOfData {0} < {1} : {2}, {3}", InputBuffer.Available(), BlockSize, CurrentFrame, EndSample);
                    return -2;
                }

                int channels = 2;
                int rc = BlockSize, len = 0;

                fixed (short* outPtr = at3OutData)
                {
                    if ((rc = Decoder.decode(InputBuffer.ReadAddr, BlockSize, outPtr, out len)) < 0)
                    {
                        Console.WriteLine($"LightCodec ERROR: {rc}");
                        Console.WriteLine($"AddStreamData 0x{InputBuffer.Available():X} / 0x{InputBuffer.CurrentSize:X} Frame {CurrentFrame}");
                        Console.WriteLine($"LoopInfoList {NumberOfLoops} Start {LoopInfoList[0].StartSample}");

                        return 0;
                    }
                }
                InputBuffer.notifyRead(BlockSize);

                int DecodedSamplesChannels = MaximumSamples * channels;

                CurrentFrame++;

                fixed (short* buf_ptr = at3OutData)
                {
                    for (int n = 0; n < DecodedSamplesChannels; n += channels)
                    {
                        SamplesOut->Left = buf_ptr[n + 0];
                        SamplesOut->Right = buf_ptr[n + 1];
                        SamplesOut++;
                    }
                }

                return rc;
            }

            void IDisposable.Dispose()
            {
                InputBuffer.Dispose();

                PrimaryBuffer.DeallocateFromParent();

                //Console.WriteLine($"Atrac {this.GetUidIndex()} Dispose");
            }

        }

    }
}