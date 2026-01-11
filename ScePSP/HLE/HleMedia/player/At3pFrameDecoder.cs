using LightCodec;
using System;
using System.IO;

namespace cscodec.h264.player
{
    public sealed unsafe class At3pFrameDecoder : FrameDecoder<short[]>
    {
        private ILightCodec FrameDecoder;

        public At3pFrameDecoder(Stream stream) : base(stream)
        {
        }

        protected override void Close()
        {
        }

        protected override void InitProtected()
        {
            FrameDecoder = CodecFactory.Get(AudioCodec.AT3plus);
        }

        protected override short[] DecodeFrameFromPacket(av.AVPacket avpkt, out int len)
        {
            Console.WriteLine("cscodec.h264.player {0}", avpkt.data_offset);
            //File.WriteAllBytes(@"samples.raw2", avpkt.data_base);

            len = 0;
            short[] samples = new short[8192];

            fixed (byte* data = &avpkt.data_base[avpkt.data_offset])
            {
                fixed(short* outptr = samples)
                    FrameDecoder.decode(data, avpkt.size, outptr, out len);

                Console.WriteLine("cscodec.h264.player AT3+ {0}, {1}", len, samples.Length);
            }

            return samples;
        }
    }
}