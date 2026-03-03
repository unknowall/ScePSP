using System;
using System.Numerics;

namespace ScePSP.Utils
{
    public static unsafe class Hashing
    {
        public static ulong FastHash(byte* pointer, int count, ulong startHash = 0)
        {
            if (pointer == null)
            {
                return startHash;
            }

            if (count > 4 * 2048 * 2048)
            {
                Console.WriteLine("FastHash too big count!");
                return startHash;
            }

            try
            {
                return FastHash_ROL(pointer, count);
            }
            catch (NullReferenceException nullReferenceException)
            {
                Console.WriteLine(nullReferenceException);
            }
            catch (AccessViolationException accessViolationException)
            {
                Console.WriteLine(accessViolationException);
            }

            return startHash;
        }

        private static unsafe ulong FastHash_ROL(byte* data, int len)
        {
            const ulong prime = 0x9e3779b97f4a7c15ul;
            ulong hash = (ulong)len * prime;

            ulong* ptr = (ulong*)data;
            int blocks = len >> 3;

            for (int i = 0; i < blocks; i++)
            {
                hash ^= ptr[i];
                hash = BitOperations.RotateLeft(hash, 13);  // rol13
                hash *= prime;
            }
            int remaining = len & 7;
            if (remaining > 0)
            {
                ulong last = 0;
                byte* bytePtr = (byte*)(ptr + blocks);
                for (int i = 0; i < remaining; i++)
                {
                    last |= (ulong)bytePtr[i] << (i << 3);
                }

                hash ^= last;
                hash = BitOperations.RotateLeft(hash, 13);
                hash *= prime;
            }
            return hash;
        }

        private static ulong FastHash_64(byte* pointer, int count, ulong startHash = 0)
        {
            var hash = startHash;

            while (count >= 8)
            {
                hash += *(ulong*)pointer + (ulong)(count << 31);
                pointer += 8;
                count -= 8;
            }

            while (count >= 1)
            {
                hash += *pointer++;
                count--;
            }

            return hash;
        }
    }
}