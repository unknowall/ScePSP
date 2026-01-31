using System;

namespace ScePSPUtils
{
    public static class BitUtils
    {
        //public static uint CreateMask(int size) => (size == 0) ? 0 : (uint) ((1 << size) - 1);

        public static uint CreateMask(this int size) => size == 32 ? 0xFFFFFFFFu : (uint)((1u << size) - 1);

        public static void Insert(ref uint value, int offset, int count, uint valueToInsert) =>
            value = Insert(value, offset, count, valueToInsert);

        public static uint Insert(this uint initialValue, int offset, int count, uint valueToInsert) =>
            InsertWithMask(initialValue, offset, CreateMask(count), valueToInsert);

        public static void
            InsertScaled(ref uint initialValue, int offset, int count, uint valueToInsert, uint maxValue) =>
            initialValue = InsertScaled(initialValue, offset, count, valueToInsert, maxValue);

        public static uint InsertScaled(this uint initialValue, int offset, int count, uint valueToInsert, uint maxValue) =>
            InsertWithMask(initialValue, offset, CreateMask(count), valueToInsert * CreateMask(count) / maxValue);

        private static uint InsertWithMask(this uint initialValue, int offset, uint mask, uint valueToInsert) =>
            (initialValue & ~(mask << offset)) | ((valueToInsert & mask) << offset);

        public static uint Extract(this uint initialValue, int offset, int count) => (initialValue >> offset) & CreateMask(count);

        public static uint ExtractScaled(this uint initialValue, int offset, int count, int scale) =>
            (uint)(Extract(initialValue, offset, count) * scale / CreateMask(count));

        public static uint ExtractScaled(this ushort initialValue, int offset, int count, int scale) =>
            ExtractScaled((uint)initialValue, offset, count, scale);

        public static bool ExtractBool(this uint initialValue, int offset) => Extract(initialValue, offset, 1) != 0;

        public static int ExtractSigned(this uint initialValue, int offset, int count)
        {
            var mask = CreateMask(count);
            var value = (initialValue >> offset) & mask;

            // 修正：检查 value 自身的最高位（第 count-1 位）
            uint signBit = 1u << (count - 1);

            if ((value & signBit) != 0)
            {
                value |= ~mask;
            }
            return (int)value;
        }

        public static float ExtractUnsignedScaled(this uint value, int offset, int count, float scale = 1.0f)
        {
            return (float)Extract(value, offset, count) / CreateMask(count) * scale;
        }

        public static byte[] XorBytes(params byte[][] arrays)
        {
            var length = arrays[0].Length;
            foreach (var array in arrays)
                if (array.Length != length) throw new InvalidOperationException("Arrays sizes must match");
            var bytes = new byte[length];
            foreach (var array in arrays)
            {
                for (var n = 0; n < length; n++) bytes[n] ^= array[n];
            }
            return bytes;
        }

        static readonly int[] MultiplyDeBruijnBitPosition =
        {
            0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
            31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
        };

        public static int GetFirstBit1(this uint v) => MultiplyDeBruijnBitPosition[(uint)((v & -v) * 0x077CB531U) >> 27];
    }
}