using ScePSP.BackEnd;
using ScePSP.Cpu;
using ScePSP.Hle.Attributes;
using ScePSP.Hle.Formats.audio;
using ScePSP.Hle.Managers;
using ScePSP.Hle.Modules.audio;
using ScePSP.Memory;
using ScePSPUtils;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ScePSP.Hle.Modules.libatrac3plus
{
    [HlePspModule(ModuleFlags = ModuleFlags.KernelMode | ModuleFlags.Flags0x00010011)]
    public unsafe partial class sceAtrac3plus : HleModuleHost
    {
        static Logger Logger = Logger.GetLogger("sceAtrac3plus");

        public sceAudio sceAudio => PSPDrivers.HleModules.sceAudio;

        public HleMemoryManager HleMemoryManager => PSPDrivers.HLE.MemoryManager;

        private Atrac TryToAlloc(Atrac Atrac)
        {
            var CodecType = Atrac.CodecType;
            var Count = PSPDrivers.HLE.HleUidPoolManager.List<Atrac>().Count(_Atrac => _Atrac.CodecType == CodecType);
            if (CodecType == CodecType.PSP_MODE_AT_3_PLUS)
            {
                if (Count >= MaxAtrac3Plus)
                {
                    throw new SceKernelException(SceKernelErrors.ATRAC_ERROR_NO_ATRACID);
                }
            }
            else if (CodecType == CodecType.PSP_MODE_AT_3)
            {
                if (Count >= MaxAtrac3)
                {
                    throw new SceKernelException(SceKernelErrors.ATRAC_ERROR_NO_ATRACID);
                }
            }

            return Atrac;
        }

        /// <summary>
        /// Creates a new Atrac ID from the specified data
        /// </summary>
        /// <param name="DataPointer">The buffer holding the atrac3 data, including the RIFF/WAVE header.</param>
        /// <param name="DataLength">The size of the buffer pointed by buf</param>
        /// <returns>The new atrac ID, or less than 0 on error </returns>
        [HlePspFunction(NID = 0x7A20E7AF, FirmwareVersion = 150)]
        [HleTrackCall]
        public Atrac sceAtracSetDataAndGetID(byte* DataPointer, int DataLength)
        {
            return TryToAlloc(new Atrac(DataPointer, DataLength));
        }

        [HlePspFunction(NID = 0xB3B5D042, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracGetOutputChannel(CpuThreadState CpuThreadState, Atrac Atrac, out int OutputChannel)
        {
            OutputChannel = sceAudio.sceAudioChReserve(CpuThreadState, -1, Atrac.MaximumSamples, PspAudio.FormatEnum.Stereo);
            return 0;
        }

        /// <summary>
        /// Gets the bitrate.
        /// </summary>
        /// <param name="AtracId">The atracID</param>
        /// <param name="Bitrate">Pointer to a integer that receives the bitrate in kbps</param>
        /// <returns>Less than 0 on error, otherwise 0</returns>
        [HlePspFunction(NID = 0xA554A158, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracGetBitrate(Atrac Atrac, out uint Bitrate)
        {
            //Bitrate = Atrac.Format.Bitrate;
            uint _AtracBitrate = (uint)(Atrac.Format.BytesPerFrame * 352800 / 1000);
            if (Atrac.CodecType == CodecType.PSP_MODE_AT_3_PLUS)
            {
                _AtracBitrate = ((_AtracBitrate >> 11) + 8) & 0xFFFFFFF0;
            }
            else
            {
                _AtracBitrate = (_AtracBitrate + 511) >> 10;
            }
            Bitrate = _AtracBitrate;
            return 0;
        }

        [HlePspFunction(NID = 0x0E2A73AB, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracSetData(Atrac Atrac, byte* BufferPointer, int BufferSizeInBytes)
        {
            Atrac.SetData(BufferPointer, BufferSizeInBytes);

            return 0;
        }

        /// <summary>
        /// Gets the maximum number of samples of the atrac3 stream.
        /// </summary>
        /// <param name="Atrac">The atrac ID</param>
        /// <param name="MaxNumberOfSamples">Pointer to a integer that receives the maximum number of samples.</param>
        /// <returns>Less than 0 on error, otherwise 0</returns>
        [HlePspFunction(NID = 0xD6A5F2F7, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracGetMaxSample(Atrac Atrac, out int MaxNumberOfSamples)
        {
            MaxNumberOfSamples = Atrac.MaximumSamples;
            return 0;
        }

        [HlePspFunction(NID = 0xFAA4F89B, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracGetLoopStatus(Atrac Atrac, out int piLoopNum, out int puiLoopStatus)
        {
            piLoopNum = Atrac.NumberOfLoops;
            puiLoopStatus = Atrac.LoopStatus;

            return 0;
        }

        /// <summary>
        /// Sets the number of loops for this atrac ID
        /// </summary>
        /// <param name="Atrac">The atracID</param>
        /// <param name="NumberOfLoops">
        ///		The number of loops to set (0 means play it one time, 1 means play it twice, 2 means play it three times, ...)
        ///		-1 means play it forever
        /// </param>
        /// <returns>Less than 0 on error, otherwise 0</returns>
        [HlePspFunction(NID = 0x868120B5, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracSetLoopNum(Atrac Atrac, int NumberOfLoops)
        {
            if (Atrac.Smpl.LoopCount == 0) throw new SceKernelException(SceKernelErrors.ATRAC_ERROR_UNSET_PARAM);

            Atrac.NumberOfLoops = NumberOfLoops;

            return 0;
        }

        /// <summary>
        /// Gets the remaining (not decoded) number of frames
        /// </summary>
        /// <param name="Atrac">The atrac ID</param>
        /// <param name="RemainFramePointer">
        ///		Pointer to a integer that receives either -1 if all at3 data is already on memory, 
        ///		or the remaining (not decoded yet) frames at memory if not all at3 data is on memory 
        /// </param>
        /// <returns>Less than 0 on error, otherwise 0</returns>
        [HlePspFunction(NID = 0x9AE849A7, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracGetRemainFrame(Atrac Atrac, out int RemainFramePointer)
        {
            RemainFramePointer = Atrac.RemainingFrames;
            return 0;
        }

        /// <summary>
        /// Decode a frame of data. 
        /// </summary>
        /// <param name="AtracId">The atrac ID</param>
        /// <param name="SamplesOut">pointer to a buffer that receives the decoded data of the current frame</param>
        /// <param name="NumberOfSamples">pointer to a integer that receives the number of audio samples of the decoded frame</param>
        /// <param name="ReachedEnd">pointer to a integer that receives a boolean value indicating if the decoded frame is the last one</param>
        /// <param name="RemainingFramesToDecode">
        ///		pointer to a integer that receives either -1 if all at3 data is already on memory, 
        ///		or the remaining (not decoded yet) frames at memory if not all at3 data is on memory
        /// </param>
        /// <returns>Less than 0 on error, otherwise 0</returns>
        [HlePspFunction(NID = 0x6A8C3CD5, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracDecodeData(Atrac Atrac, StereoShortSoundSample* SamplesOut,
            [HleInvalidAsInvalidPointer] out int NumberOfSamples, [HleInvalidAsInvalidPointer] out int ReachedEnd,
            [HleInvalidAsInvalidPointer] out int RemainingFramesToDecode)
        {
            return _sceAtracDecodeData(Atrac, SamplesOut, out NumberOfSamples, out ReachedEnd, out RemainingFramesToDecode);
        }

        private int _sceAtracDecodeData(Atrac Atrac, StereoShortSoundSample* SamplesOut, out int NumberOfSamples, out int ReachedEnd, out int RemainingFramesToDecode)
        {
            ReachedEnd = 0;
            NumberOfSamples = 0;
            RemainingFramesToDecode = Atrac.RemainingFrames;

            if (Atrac.SecondBufferNeeded && !Atrac.SecondBufferSet)
            {
                Console.WriteLine($"sceAtracDecodeData {Atrac} needs second buffer!");
                return (int)SceKernelErrors.ERROR_ATRAC_SECOND_BUFFER_NEEDED;
            }

            int Ret = Atrac.Decode(SamplesOut);
            if (Ret == -1)
            {
                NumberOfSamples = 0;
                return 0;
            }
            if (Ret == -2)
            {
                return (int)SceKernelErrors.ERROR_ATRAC_BUFFER_IS_EMPTY;
            }

            NumberOfSamples = Atrac.Decoder.NumberOfSamples;
            RemainingFramesToDecode = Atrac.RemainingFrames;

            //Console.WriteLine("{0}/{1} -> {2} : {3}", Atrac.DecodingOffset, Atrac.DataStream.Length, NumberOfSamples, Atrac.DecodingReachedEnd);

            if (Atrac.DecodingReachedEnd)
            {
                if (Atrac.NumberOfLoops == 0)
                {
                    NumberOfSamples = 0;
                    ReachedEnd = 1;
                    RemainingFramesToDecode = 0;
                    //Console.WriteLine("SceKernelErrors.ERROR_ATRAC_ALL_DATA_DECODED");
                    return (int)(SceKernelErrors.ERROR_ATRAC_ALL_DATA_DECODED);
                }
                if (Atrac.NumberOfLoops > 0) Atrac.NumberOfLoops--;

                Atrac.SetPlayPosition(Atrac.LoopInfoList.Length > 0 ? Atrac.LoopInfoList[0].StartSample : 0, Atrac.InputBuffer.CurrentSize, 0);

                if (Atrac.InputBuffer.Position == Atrac.InputBuffer.CurrentSize)
                    Atrac.SetPlayPosition(0, Atrac.InputBuffer.CurrentSize, 0);

                //Console.WriteLine($"LoopInfoList {Atrac.NumberOfLoops} Start {Atrac.LoopInfoList[0].StartSample} Set 0x{Atrac.InputBuffer.Available():X}");
            }

            // Delay the thread decoding the Atrac data,
            // the thread is also blocking using semaphores/event flags on a real PSP.
            if (Ret > 0)
            {
                PSPDrivers.HLE.ThreadManForUser.sceKernelDelayThread(2300); // Microseconds, based on PSP tests
            }

            return 0;
        }

        /// <summary>
        /// It releases an atrac ID
        /// </summary>
        /// <param name="AtracId">The atrac ID to release</param>
        /// <returns>Less than 0 on error</returns>
        [HlePspFunction(NID = 0x61EB33F5, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracReleaseAtracID(Atrac Atrac)
        {
            Atrac.RemoveUid();
            return 0;
        }

        [HlePspFunction(NID = 0x780F88D1, FirmwareVersion = 150)]
        [HleTrackCall]
        public Atrac sceAtracGetAtracID(CodecType CodecType)
        {
            if (CodecType != CodecType.PSP_MODE_AT_3 && CodecType != CodecType.PSP_MODE_AT_3_PLUS)
            {
                throw new SceKernelException(SceKernelErrors.ATRAC_ERROR_INVALID_CODECTYPE);
            }

            return TryToAlloc(new Atrac(CodecType));
        }

        /// <summary>
        /// Gets the number of samples of the next frame to be decoded.
        /// </summary>
        /// <param name="AtracId">The atrac ID</param>
        /// <param name="NumberOfSamplesInNextFrame">Pointer to receives the number of samples of the next frame.</param>
        /// <returns>Less than 0 on error, otherwise 0</returns>
        [HlePspFunction(NID = 0x36FAABFB, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracGetNextSample(Atrac Atrac, out int NumberOfSamplesInNextFrame)
        {
            NumberOfSamplesInNextFrame = 0;
            try
            {
                NumberOfSamplesInNextFrame = Atrac.CurrentFrame + 1;

                return 0;
            }
            catch (Exception Exception)
            {
                NumberOfSamplesInNextFrame = 1;
                Console.Error.WriteLine(Exception);
                return 0;
            }
        }

        [HlePspFunction(NID = 0xE88F759B, FirmwareVersion = 150)]
        [HleTrackCall(Notice = false)]
        public int sceAtracGetInternalErrorInfo(Atrac Atrac, out int ErrorResult)
        {
            ErrorResult = 0;
            return 0;
        }

        /// <param name="AtracId">The atrac ID</param>
        /// <param name="WritePointerPointer">Pointer to where to read the atrac data</param>
        /// <param name="AvailableBytes">Number of bytes available at the writePointer location</param>
        /// <param name="ReadOffset">Offset where to seek into the atrac file before reading</param>
        /// <returns>Less than 0 on error, otherwise 0</returns>
        [HlePspFunction(NID = 0x5D268707, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracGetStreamDataInfo(Atrac Atrac, out PspPointer WritePointerPointer, out int AvailableBytes, out int ReadOffset)
        {
            //Console.WriteLine($"sceAtracGetStreamDataInfo 0x{Atrac.InputBuffer.CurrentSize:X} 0x{Atrac.InputBuffer.AvailableWriteSize():X}");

            WritePointerPointer = Memory.PointerToPspPointer(Atrac.PrimaryBuffer.LowPointer);
            AvailableBytes = Atrac.InputBuffer.AvailableWriteSize();
            ReadOffset = Atrac.InputBuffer.CurrentSize;

            return 0;
        }

        /// <param name="AtracId">The atrac ID</param>
        /// <param name="bytesToAdd">Number of bytes read into location given by sceAtracGetStreamDataInfo().</param>
        /// <returns>Less than 0 on error, otherwise 0</returns>
        [HlePspFunction(NID = 0x7DB31251, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracAddStreamData(Atrac Atrac, int bytesToAdd)
        {
            //Console.WriteLine($"sceAtracAddStreamData 0x{bytesToAdd:X}");

            Atrac.AddStreamData(bytesToAdd);

            return 0;
        }

        [HlePspFunction(NID = 0x83E85EA0, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracGetSecondBufferInfo(Atrac Atrac, out uint puiPosition, out uint puiDataByte)
        {
            //Console.WriteLine("sceAtracGetSecondBufferInfo");

            if (!Atrac.SecondBufferNeeded)
            {
                // PSP clears both values when returning this error code.
                puiPosition = 0;
                puiDataByte = 0;
                return (int)SceKernelErrors.ERROR_ATRAC_SECOND_BUFFER_NOT_NEEDED;
            }

            puiPosition = Atrac.SecondBufferPosition;
            puiDataByte = Atrac.SecondBufferSize;

            return 0;
        }

        /// <returns>0 - not needed ; 1 - needed</returns>
        [HlePspFunction(NID = 0xECA32A99, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracIsSecondBufferNeeded(Atrac Atrac)
        {
            Console.WriteLine("sceAtracIsSecondBufferNeeded)");

            return Atrac.SecondBufferNeeded ? 1 : 0;
        }

        [HlePspFunction(NID = 0x83BF7AFD, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracSetSecondBuffer(Atrac Atrac, byte* SecondBufferAddr, uint uiSecondBufferByte)
        {
            Console.WriteLine($"sceAtracSetSecondBuffer Szie 0x{uiSecondBufferByte:X})");

            Atrac.SetSecondBuffer(SecondBufferAddr, uiSecondBufferByte);

            return 0;
        }

        [HlePspFunction(NID = 0xE23E3A35, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracGetNextDecodePosition(Atrac Atrac, out int SamplePosition)
        {
            SamplePosition = Atrac.CurrentFrame;

            if (Atrac.DecodingReachedEnd)
            {
                return (int)SceKernelErrors.ERROR_ATRAC_ALL_DATA_DECODED;
            }

            return 0;
        }

        [HlePspFunction(NID = 0xA2BBA8BE, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracGetSoundSample(Atrac Atrac, int* EndSamplePointer, int* LoopStartSamplePointer, int* LoopEndSamplePointer)
        {
            var HasLoops = Atrac.LoopInfoList != null && Atrac.LoopInfoList.Length > 0;
            if (EndSamplePointer != null) *EndSamplePointer = Atrac.Fact.EndSample;
            if (LoopStartSamplePointer != null)
                *LoopStartSamplePointer = HasLoops ? Atrac.LoopInfoList[0].StartSample : -1;
            if (LoopEndSamplePointer != null) *LoopEndSamplePointer = HasLoops ? Atrac.LoopInfoList[0].EndSample : -1;
            return 0;
        }

        [HlePspFunction(NID = 0xCA3CA3D2, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracGetBufferInfoForReseting(Atrac Atrac, uint uiSample, PspBufferInfo* pBufferInfo)
        {
            throw new NotImplementedException();
        }

        [HlePspFunction(NID = 0x644E5607, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracResetPlayPosition(Atrac Atrac, int uiSample, int uiWriteByteFirstBuf, int uiWriteByteSecondBuf)
        {
            Atrac.SetPlayPosition(uiSample, uiWriteByteFirstBuf, uiWriteByteSecondBuf);
            return 0;
        }

        [HlePspFunction(NID = 0x2DD3E298, FirmwareVersion = 250)]
        [HleTrackCall]
        public int sceAtracGetBufferInfoForResetting(Atrac Atrac, uint uiSample, void* BufferInfoAddr)
        {
            return 0;
        }

        [HlePspFunction(NID = 0x31668baa, FirmwareVersion = 250)]
        [HleTrackCall]
        public int sceAtracGetChannel(Atrac Atrac, out int Channels)
        {
            Channels = Atrac.Format.AtracChannels;
            return 0;
        }

        private const int MAX_PSP_NUM_ATRAC_IDS = 6;
        int MaxAtrac3Plus = 2;
        int MaxAtrac3 = 2;

        [HlePspFunction(NID = 0x132F1ECA, FirmwareVersion = 250)]
        [HleTrackCall]
        public int sceAtracReinit(int at3Count, int at3plusCount)
        {
            PSPDrivers.HLE.HleUidPoolManager.RemoveAll<Atrac>();

            int Space = MAX_PSP_NUM_ATRAC_IDS;
            MaxAtrac3Plus = 0;
            MaxAtrac3 = 0;
            for (int n = 0; n < at3plusCount; n++)
            {
                if (Space >= 2)
                {
                    Space -= 2;
                    MaxAtrac3Plus++;
                }
                else
                {
                    throw new SceKernelException(SceKernelErrors.ERROR_OUT_OF_MEMORY);
                }
            }
            for (int n = 0; n < at3Count; n++)
            {
                if (Space >= 1)
                {
                    Space -= 1;
                    MaxAtrac3++;
                }
                else
                {
                    throw new SceKernelException(SceKernelErrors.ERROR_OUT_OF_MEMORY);
                }
            }
            return 0;
        }

        [HlePspFunction(NID = 0x3F6E26B5, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracSetHalfwayBuffer(Atrac Atrac, byte* halfBuffer, int readSize, int halfBufferSize)
        {
            Atrac.SetData(halfBuffer, halfBufferSize, readSize);

            return 0;
        }

        [HlePspFunction(NID = 0x5CF9D852, FirmwareVersion = 250)]
        [HleTrackCall]
        public int sceAtracSetMOutHalfwayBuffer(Atrac Atrac, byte* MOutHalfBuffer, int readSize, int MOutHalfBufferSize)
        {
            Atrac.SetData(MOutHalfBuffer, MOutHalfBufferSize, readSize, true);

            return 0;
        }

        [HlePspFunction(NID = 0x0FAE370E, FirmwareVersion = 150)]
        [HleTrackCall]
        public Atrac sceAtracSetHalfwayBufferAndGetID(byte* HalfBufferPointer, int readSize, int HalfBufferSize)
        {
            return TryToAlloc(new Atrac(HalfBufferPointer, HalfBufferSize, readSize));
        }

        [HlePspFunction(NID = 0x9CD7DE03, FirmwareVersion = 250)]
        [HleTrackCall]
        public Atrac sceAtracSetMOutHalfwayBufferAndGetID(byte* halfBuffer, int readSize, int halfBufferSize)
        {
            return TryToAlloc(new Atrac(halfBuffer, halfBufferSize, readSize, true));
        }

        [HlePspFunction(NID = 0x5DD66588, FirmwareVersion = 250)]
        [HleTrackCall]
        public int sceAtracSetAA3HalfwayBufferAndGetID(byte* halfBuffer, uint readSize, uint halfBufferSize)
        {
            throw new SceKernelException((SceKernelErrors)(-1));
        }

        [HlePspFunction(NID = 0x5622B7C1, FirmwareVersion = 250)]
        [HleTrackCall]
        public Atrac sceAtracSetAA3DataAndGetID(byte* buffer, int bufferSize, int fileSize, uint metadataSizeAddr)
        {
            throw new SceKernelException((SceKernelErrors)(-1));
        }

        [HlePspFunction(NID = 0xD5C28CC0, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracEndEntry()
        {
            return 0;
        }

        [HlePspFunction(NID = 0xD1F59FDB, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceAtracStartEntry()
        {
            return 0;
        }

        [HlePspFunction(NID = 0x231FC6B7, FirmwareVersion = 600)]
        [HleTrackCall]
        public int _sceAtracGetContextAddress(int atID)
        {
            return 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct LowLevelParam
        {
            public int numberOfChannels;
            public int outputChannels;
            public int sourceBufferLength;
        }

        [HlePspFunction(NID = 0x1575D64B, FirmwareVersion = 620)]
        [HleTrackCall]
        public int sceAtracLowLevelInitDecoder(Atrac Atrac, LowLevelParam* param)
        {
            Atrac.Decoder.init(param->sourceBufferLength, param->numberOfChannels, param->outputChannels, 0);
            return 0;
        }

        [HlePspFunction(NID = 0x0C116E1B, FirmwareVersion = 620)]
        [HleTrackCall]
        public int sceAtracLowLevelDecode(int atID, byte* sourceAddr, PspPointer sourceBytesConsumedAddr, byte* samplesAddr, PspPointer sampleBytesAddr)
        {
            return 0;
        }
    }
}