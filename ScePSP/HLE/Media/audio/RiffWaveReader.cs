using ScePSPUtils.Extensions;
using ScePSPUtils.Streams;
using System;
using System.IO;

namespace ScePSP.Hle.Formats.audio
{
    public class RiffWaveReader
    {
        public event Action<string, uint, SliceStream> HandleChunk;

        public int HeadSize = 0;

        public bool HasData = false;

        public RiffWaveReader()
        {
        }

        public void Parse(Stream Stream)
        {
            ParseFile(Stream);
        }

        public void ParseFile(Stream Stream)
        {
            if (Stream.ReadString(4) != "RIFF") throw new InvalidDataException("Not a RIFF File");
            var RiffSize = new BinaryReader(Stream).ReadUInt32();
            HeadSize += 8;
            var RiffStream = Stream.ReadStream(RiffSize);
            ParseRiff(RiffStream);
        }

        public void ParseRiff(Stream Stream)
        {
            if (Stream.ReadString(4) != "WAVE") throw new InvalidDataException("Not a RIFF.WAVE File");
            HeadSize += 4;
            while (!Stream.Eof() && !HasData)
            {
                var ChunkType = Stream.ReadString(4);
                uint ChunkSize = new BinaryReader(Stream).ReadUInt32();
                HeadSize += 8;
                if (ChunkType == "data")
                {
                    HasData = true;
                }
                else
                {
                    HeadSize += (int)ChunkSize;
                }
                var ChunkStream = Stream.ReadStream(ChunkSize);
                HandleChunkInternal(ChunkType, ChunkSize, ChunkStream);
            }
        }

        public void HandleChunkInternal(string ChunkType, uint ChunkSize, SliceStream ChunkStream)
        {
            if (HandleChunk != null) HandleChunk(ChunkType, ChunkSize, ChunkStream);
        }
    }
}