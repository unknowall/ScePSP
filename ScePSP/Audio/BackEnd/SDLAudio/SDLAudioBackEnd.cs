using System;
using System.Collections.Concurrent;
using static SDL2.SDL;

namespace ScePSP.BackEnd.SDL
{
    public unsafe class SDLAudioBackEnd : AudioBackEnd
    {
        private static uint audiodeviceid;
        private SDL_AudioCallback audioCallbackDelegate;

        private ConcurrentQueue<short[]> Queue = new ConcurrentQueue<short[]>();

        public const int Frequency = 44100;
        public const double SamplesPerMillisecond = (double)Frequency / 500;

        public const int NumberOfBuffers = 4;
        public const int NumberOfChannels = 2;
        public const int BufferMilliseconds = 20;
        public const int SamplesPerBuffer = (int)(SamplesPerMillisecond * BufferMilliseconds * NumberOfChannels);

        public SDLAudioBackEnd()
        {
            SDL_Init(SDL_INIT_AUDIO);

            audioCallbackDelegate = AudioCallback;

            SDL_AudioSpec desired = new SDL_AudioSpec
            {
                channels = NumberOfChannels,
                format = AUDIO_S16,
                freq = Frequency,
                samples = SamplesPerBuffer / NumberOfChannels,
                callback = audioCallbackDelegate,
                userdata = IntPtr.Zero

            };
            SDL_AudioSpec obtained = new SDL_AudioSpec();

            audiodeviceid = SDL_OpenAudioDevice(null, 0, ref desired, out obtained, 0);

            if (audiodeviceid != 0)
                SDL_PauseAudioDevice(audiodeviceid, 0);
        }

        ~SDLAudioBackEnd()
        {
            Stop();
        }

        private unsafe void AudioCallback(IntPtr userdata, IntPtr stream, int len)
        {
            int requiredSamples = len / sizeof(short);
            var streamSpan = new Span<short>((void*)stream, requiredSamples);
            streamSpan.Fill(0);

            if (Queue.Count == 0)
            {
                return;
            }

            int filledSamples = 0;
            while (filledSamples < requiredSamples && Queue.TryDequeue(out var buffer))
            {
                if (buffer == null || buffer.Length == 0)
                {
                    continue;
                }
                int copyCount = Math.Min(buffer.Length, requiredSamples - filledSamples);
                new Span<short>(buffer, 0, copyCount).CopyTo(streamSpan.Slice(filledSamples));
                filledSamples += copyCount;
            }
        }

        public override void Update(Action<short[]> readStream)
        {
            while (Queue.Count < 2)
            {
                var Data = new short[SamplesPerBuffer / 2];
                readStream(Data);
                //for (int n = 0; n < Data.Length; n++) Console.Write(Data[n]);
                Queue.Enqueue(Data);
            }
        }

        public override void Pause()
        {
            if (audiodeviceid != 0)
                SDL_PauseAudioDevice(audiodeviceid, 1);
        }

        public override void Resume()
        {
            if (audiodeviceid != 0)
                SDL_PauseAudioDevice(audiodeviceid, 0);
        }

        public override void Stop()
        {
            if (audiodeviceid != 0)
            {
                SDL_PauseAudioDevice(audiodeviceid, 0);
                SDL_CloseAudioDevice(audiodeviceid);

                audiodeviceid = 0;
            }
        }
    }
}