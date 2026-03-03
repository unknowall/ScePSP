using ScePSP.Memory;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ScePSP.Hle.Modules._unknownPrx //vsh/module/vshmain.prx vsh_module
{
    public unsafe partial class sceCcc : HleModuleHost
    {
        protected internal unsafe string getStringUTF8(uint addr)
        {
            int* ptr = (int*)Memory.PspAddressToPointerUnsafe(addr);
            int* tempPtr = ptr;
            int length = 0;
            while (*tempPtr != 0)
            {
                length++;
                tempPtr++;
            }
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = (byte)ptr[i];
            }
            return Encoding.UTF8.GetString(bytes);
        }

        [HlePspFunction(NID = 0x92C05851, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceCccEncodeUTF8(PspPointer dstAddrUTF8, int ucs4char)
        {
            return 0;
        }

        [HlePspFunction(NID = 0xB7D3C112, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceCccStrlenUTF8(PspPointer srcAddrUTF8)
        {
            return 0;
        }

        [HlePspFunction(NID = 0xC6A8BEE2, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceCccDecodeUTF8(PspPointer srcAddrUTF8)
        {
            string srcString = Memory.ReadStringz(srcAddrUTF8.Address, Encoding.UTF8);

            Console.WriteLine(string.Format("sceCccDecodeUTF8 string='{0}'(0x{1:X8}), size={2:D})", srcString, srcAddrUTF8.Address, srcString.Length));

            srcAddrUTF8.Address = (uint)(srcAddrUTF8.Address + srcString.Length);

            return srcString.Length;
        }
    }
}
