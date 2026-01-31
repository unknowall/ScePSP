using ScePSP.Crypto;
using ScePSPUtils;
using ScePSPUtils.Extensions;
using ScePSPUtils.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ScePSP.Core
{
    // Token: 0x02000057 RID: 87
    public class IplReader
    {
        // Token: 0x06000171 RID: 369 RVA: 0x00068694 File Offset: 0x00066894
        public IplReader(NandReader NandReader)
        {
            this.Stream = NandReader.SliceWithLength(0L, -1L, null);
        }

        // Token: 0x06000172 RID: 370 RVA: 0x000686C0 File Offset: 0x000668C0
        public MemoryStream GetIplData()
        {
            MemoryStream memoryStream = new MemoryStream();
            foreach (ushort num in this.GetIplOffsets().ToArray<ushort>())
            {
                this.Stream.Position = (long)(16384 * num);
                memoryStream.WriteBytes(this.Stream.ReadBytes(16384));
            }
            memoryStream.Position = 0L;
            return memoryStream;
        }

        // Token: 0x06000173 RID: 371 RVA: 0x00046B4A File Offset: 0x00044D4A
        public IplReader.IplInfo LoadIplToMemory(Stream OutputStream)
        {
            return IplReader.DecryptIplToMemory(this.GetIplData().ToArray().Skip(16384).ToArray<byte>(), OutputStream, true);
        }

        // Token: 0x06000174 RID: 372 RVA: 0x00046B6D File Offset: 0x00044D6D
        public void WriteIplToFile(Stream StreamOut)
        {
            IplReader.DecryptIplToMemory(this.GetIplData().ToArray().Skip(16384).ToArray<byte>(), StreamOut, false);
        }

        // Token: 0x06000175 RID: 373 RVA: 0x00068728 File Offset: 0x00066928
        public unsafe static IplReader.IplInfo DecryptIplToMemory(byte[] IplData, Stream OutputStream, bool ToMemoryAddress = true)
        {
            byte[] array = new byte[4096];
            IplReader.IplInfo result = default(IplReader.IplInfo);
            fixed (byte* ptr = IplData)
            {
                fixed (byte* ptr2 = array)
                {
                    for (int i = 0; i < IplData.Length; i += 4096)
                    {
                        byte* ptr3 = ptr + i;
                        Kirk.AES128CMACHeader aes128CMACHeader = *(Kirk.AES128CMACHeader*)ptr3;
                        Kirk kirk = new Kirk();
                        kirk.kirk_init();
                        kirk.kirk_CMD1(ptr2, ptr3, 4096, false);
                        IplReader.IplBlock iplBlock = *(IplReader.IplBlock*)ptr2;
                        if (ToMemoryAddress)
                        {
                            OutputStream.Position = (long)((ulong)iplBlock.LoadAddress);
                            Console.WriteLine("IplBlock.LoadAddress: 0x{0:X8}", iplBlock.LoadAddress);
                        }
                        OutputStream.WriteBytes(PointerUtils.PointerToByteArray(&iplBlock.BlockData, (int)iplBlock.BlockSize));
                        if (iplBlock.EntryFunction != 0u)
                        {
                            result.EntryFunction = iplBlock.EntryFunction;
                        }
                    }
                }
            }
            return result;
        }

        // Token: 0x06000176 RID: 374 RVA: 0x0006884C File Offset: 0x00066A4C
        public IEnumerable<ushort> GetIplOffsets()
        {
            SliceStream Stream = this.Stream.SliceWithLength(65536L, -1L, null);
            for (; ; )
            {
                ushort Result = Stream.ReadStruct<ushort>();
                if (Result == 0)
                {
                    break;
                }
                yield return Result;
            }
            yield break;
        }

        // Token: 0x040001C3 RID: 451
        protected Stream Stream;

        // Token: 0x02000058 RID: 88
        [StructLayout(LayoutKind.Sequential, Size = 3936)]
        public struct IplBlock
        {
            // Token: 0x040001C4 RID: 452
            public uint LoadAddress;

            // Token: 0x040001C5 RID: 453
            public uint BlockSize;

            // Token: 0x040001C6 RID: 454
            public uint EntryFunction;

            // Token: 0x040001C7 RID: 455
            public uint Checksum;

            // Token: 0x040001C8 RID: 456
            public byte BlockData;
        }

        // Token: 0x02000059 RID: 89
        public struct IplInfo
        {
            // Token: 0x040001C9 RID: 457
            public uint EntryFunction;
        }
    }
}
