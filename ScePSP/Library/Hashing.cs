using System;

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
                return FastHash_64(pointer, count, startHash);
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