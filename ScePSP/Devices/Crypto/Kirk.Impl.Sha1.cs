using ScePSPUtils;
using System.Security.Cryptography;

namespace ScePSP.Crypto
{
    public unsafe partial class Kirk
    {
        public struct KIRK_SHA1_HEADER
        {
            public int DataSize;
        }

        public void KirkSha1(byte* OutputBuffer, byte* InputBuffer, int InputSize)
        {
            //CheckInitialized();

            var Header = (KIRK_SHA1_HEADER*)InputBuffer;
            if (InputSize == 0 || Header->DataSize == 0)
            {
                throw (new KirkException(ResultEnum.PSP_KIRK_DATA_SIZE_IS_ZERO));
            }

            //Size <<= 4;
            //Size >>= 4;
            InputSize &= 0x0FFFFFFF;
            InputSize = (InputSize < Header->DataSize) ? InputSize : Header->DataSize;

            var Sha1Hash = Sha1(
                PointerUtils.PointerToByteArray(InputBuffer + 4, InputSize)
            );

            PointerUtils.Memcpy(OutputBuffer, Sha1Hash, Sha1Hash.Length);
        }

        public static byte[] Sha1(byte[] Input)
        {
            return (new SHA1CryptoServiceProvider()).ComputeHash(Input);
        }
    }
}
