using System;
using System.Linq;
using System.Runtime.InteropServices;
using ScePSP.Utils;
using static SDL2.SDL;

namespace ScePSP.Core.Audio.Impl.SDL
{
    public unsafe class SDLAudioImpl : PspAudioImpl
    {
        private static uint audiodeviceid;
        private SDL_AudioCallback audioCallbackDelegate;
        private CircularBuffer<short> SamplesBuffer;
        private static int bufferms = 50;

        public const int Frequency = 44100;
        public const double SamplesPerMillisecond = (double)Frequency / 500;

        public const int NumberOfBuffers = 4;
        public const int NumberOfChannels = 2;
        public const int BufferMilliseconds = 10;
        public const int SamplesPerBuffer = (int)(SamplesPerMillisecond * BufferMilliseconds * NumberOfChannels);

        public SDLAudioImpl()
        {
            SDL_Init(SDL_INIT_AUDIO);

            audioCallbackDelegate = AudioCallbackImpl;

            SDL_AudioSpec desired = new SDL_AudioSpec
            {
                channels = 2,
                format = AUDIO_S16LSB,
                freq = 44100,
                samples = 2048,
                callback = audioCallbackDelegate,
                userdata = IntPtr.Zero

            };
            SDL_AudioSpec obtained = new SDL_AudioSpec();

            audiodeviceid = SDL_OpenAudioDevice(null, 0, ref desired, out obtained, 0);

            int alignedSize = ((bufferms * 176 + 2048 - 1) / 2048) * 2048;

            SamplesBuffer = new CircularBuffer<short>(alignedSize / 2); // 300 ms = 52920

            if (audiodeviceid != 0)
                SDL_PauseAudioDevice(audiodeviceid, 0);
        }

        ~SDLAudioImpl()
        {
            if (audiodeviceid != 0)
                SDL_CloseAudioDevice(audiodeviceid);
        }

        private unsafe void AudioCallbackImpl(IntPtr userdata, IntPtr stream, int len)
        {
            int shortlen = len / 2;
            short[] tempBuffer = new short[shortlen];

            int shortsRead = SamplesBuffer.Read(tempBuffer, 0, shortlen);

            fixed (short* ptr = tempBuffer)
            {
                System.Buffer.MemoryCopy(ptr, (void*)stream, shortlen, shortsRead);
            }

            if (shortsRead < shortlen)
            {
                new Span<short>((void*)(stream + shortsRead), shortlen - shortsRead).Fill(0);
            }
        }

        private short[] _bufferData;

        public override void Update(Action<short[]> readStream)
        {
            const int readSamples = SamplesPerBuffer;
            if (_bufferData == null || _bufferData.Length != readSamples)
            {
                _bufferData = new short[readSamples];
                //Console.WriteLine("Created buffer");
            }

            readStream?.Invoke(_bufferData);
            //if (_bufferData.Any(Item => Item != 0)) foreach (var C in _bufferData) Console.Write("{0},", C);

            SamplesBuffer.Write(_bufferData);
        }

        public override void StopSynchronized()
        {
            if (audiodeviceid != 0)
                SDL_PauseAudioDevice(audiodeviceid, 1);
        }

        public override bool IsWorking
        {
            get
            {
                return (audiodeviceid != 0);
            }
        }
    }
}