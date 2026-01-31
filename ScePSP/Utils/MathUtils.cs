using System;

namespace ScePSPUtils
{
    public static class MathUtils
    {
        public static T Clamp<T>(T value, T min, T max) where T : IComparable
        {
            if (value.CompareTo(min) < 0) return min;
            if (value.CompareTo(max) > 0) return max;
            return value;
        }

        public static float Lerp(float start, float end, float percent)
        {
            return start + percent * (end - start);
        }

        public static float SmoothStep(float edge0, float edge1, float x)
        {
            var t = Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
            return t * t * (3.0f - 2.0f * t);
        }

        public static void NormalizeMax(ref float[] items)
        {
            var max = Max(items);
            for (var n = 0; n < items.Length; n++) items[n] /= max;
        }

        public static void NormalizeMax(ref float a, ref float b)
        {
            var div = Math.Max(a, b);
            a /= div;
            b /= div;
        }

        public static void NormalizeSum(ref float a, ref float b)
        {
            var div = a + b;
            a /= div;
            b /= div;
        }

        public static float FastClamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static int FastClamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static byte FastClampToByte(int value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return (byte)value;
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        public static ushort ByteSwap(ushort value)
        {
            return (ushort)((value >> 8) | (value << 8));
        }

        public static uint ByteSwap(uint value)
        {
            return ((uint)ByteSwap((ushort)(value >> 0)) << 16) |
                   ((uint)ByteSwap((ushort)(value >> 16)) << 0);
        }

        public static ulong ByteSwap(ulong value)
        {
            return ((ulong)ByteSwap((uint)(value >> 0)) << 32) |
                   ((ulong)ByteSwap((uint)(value >> 32)) << 0);
        }

        public static unsafe float ByteSwap(float value)
        {
            var valueSw = ByteSwap(*(uint*)&value);
            return *(float*)&valueSw;
        }

        public static long Align(long value, long alignValue)
        {
            if (value % alignValue != 0)
            {
                value += alignValue - value % alignValue;
            }
            return value;
        }

        public static long RequiredBlocks(long size, long blockSize)
        {
            if (size % blockSize != 0)
            {
                return size / blockSize + 1;
            }
            else
            {
                return size / blockSize;
            }
        }

        public static uint PrevAligned(uint value, int alignment)
        {
            if (value % alignment != 0)
            {
                value -= (uint)(value % alignment);
            }
            return value;
        }

        public static uint NextAligned(uint value, int alignment)
        {
            return (uint)NextAligned((long)value, alignment);
        }

        public static long NextAligned(long value, long alignment)
        {
            if (alignment != 0 && value % alignment != 0)
            {
                value += alignment - value % alignment;
            }
            return value;
        }

        public static int NextPowerOfTwo(int baseValue)
        {
            var nextPowerOfTwoValue = 1;
            while (nextPowerOfTwoValue < baseValue) nextPowerOfTwoValue <<= 1;
            return nextPowerOfTwoValue;
        }

        public static float Max(params float[] items)
        {
            var maxValue = items[0];
            foreach (var item in items) if (maxValue < item) maxValue = item;
            return maxValue;
        }

        public static int Max(params int[] items)
        {
            var maxValue = items[0];
            foreach (var item in items) if (maxValue < item) maxValue = item;
            return maxValue;
        }

        public static uint Max(params uint[] items)
        {
            var maxValue = items[0];
            foreach (var item in items) if (maxValue < item) maxValue = item;
            return maxValue;
        }

        public static uint NumberOfSetBits(uint i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        public static bool IsPowerOfTwo(uint value)
        {
            return (value & (value - 1)) == 0;
            //return NumberOfSetBits(Value) == 1;
        }
    }
}