using System;

namespace ScePSPUtils
{
    public sealed unsafe class MathFloat
    {
        public static float Abs(float value)
        {
            if (value == -0f) return 0f;
            return value >= 0 ? value : -value;
        }

        public static int Cast(float value)
        {
            if (float.IsNegativeInfinity(value)) return int.MinValue;
            if (float.IsInfinity(value) || float.IsNaN(value)) return int.MaxValue;
            return (int)value;
        }

        public static int Floor(float value)
        {
            if (float.IsNegativeInfinity(value)) return int.MinValue;
            if (float.IsInfinity(value) || float.IsNaN(value)) return int.MaxValue;
            return (int)Math.Floor(value);
        }

        public static int Ceil(float value)
        {
            if (float.IsNegativeInfinity(value)) return int.MinValue;
            if (float.IsInfinity(value) || float.IsNaN(value)) return int.MaxValue;
            return (int)Math.Ceiling(value);
        }

        public static int Round(float value)
        {
            if (float.IsNegativeInfinity(value)) return int.MinValue;
            if (float.IsInfinity(value) || float.IsNaN(value)) return int.MaxValue;
            return (int)Math.Round(value);
        }

        public static float Rint(float value)
        {
            if (float.IsNegativeInfinity(value)) return int.MinValue;
            if (float.IsInfinity(value) || float.IsNaN(value)) return int.MaxValue;
            return Round(value);
        }

        public static uint ReinterpretFloatAsUInt(float value)
        {
            return *(uint*)&value;
        }

        public static float ReinterpretUIntAsFloat(uint value)
        {
            return *(float*)&value;
        }

        public static int ReinterpretFloatAsInt(float value)
        {
            return *(int*)&value;
        }

        public static float ReinterpretIntAsFloat(int value)
        {
            return *(float*)&value;
        }

        public static float Cos(float angle)
        {
            return (float)Math.Cos(angle);
        }

        public static float Sin(float angle)
        {
            return (float)Math.Sin(angle);
        }

        public static float CosV1(float angleV1)
        {
            return Cos(angleV1 * Pi2);
        }

        private const float Pi2 = (float)(Math.PI / 2f);

        public static float SinV1(float angleV1)
        {
            return Sin(angleV1 * Pi2);
        }

        public static float Clamp(float value, float min, float max)
        {
            //if (min == 0) return 0; <- DEAD!!!!
            if (float.IsNaN(value)) return value;
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static int ClampInt(int value, int min, int max)
        {
            if (value < min) value = min;
            else if (value > max) value = max;
            return value;
        }

        public static float Sqrt(float value)
        {
            return (float)Math.Sqrt(value);
        }

        public static float Scalb(float value, int count)
        {
            return (float)(value * Math.Pow(2.0f, count));
        }

        public static float Sign(float value)
        {
            if (value == 0) return 0f;
            var iValue = ReinterpretFloatAsUInt(value);
            return (iValue & 0x80000000) != 0 ? -1f : +1f;
        }

        public static float Min(float left, float right)
        {
            return Math.Min(left, right);
        }

        public static float Max(float left, float right)
        {
            return Math.Max(left, right);
        }

        public static bool IsNan(float value)
        {
            //return float.IsNaN(Value);
            return float.IsNaN(Math.Abs(value));
        }

        public static bool IsInfinity(float value)
        {
            return float.IsInfinity(value);
        }

        public static float RSqrt(float value)
        {
            return 1.0f / Sqrt(value);
        }

        public static float Asin(float value)
        {
            return (float)Math.Asin(value);
        }

        public static float AsinV1(float value)
        {
            return Asin(value) / Pi2;
        }

        public static float Vsat0(float value)
        {
            return Clamp(value, 0.0f, 1.0f);
        }

        public static float Vsat1(float value)
        {
            return float.IsNaN(value) ? value : Clamp(value, -1.0f, 1.0f);
        }

        public static float Log2(float value)
        {
            return (float)(Math.Log(value) / Math.Log(2.0f));
        }

        public static float Exp2(float value)
        {
            return (float)Math.Pow(2.0, value);
        }

        public static float NRcp(float value)
        {
            return -(1.0f / value);
        }

        public static float NSinV1(float angle)
        {
            var value = SinV1(angle);
            if (value == 0f) return -0f;
            return -value;
        }

        public static float RExp2(float value)
        {
            return (float)(1.0 / Math.Pow(2.0, value));
        }

        public static bool IsNanOrInfinity(float value)
        {
            return IsNan(value) || float.IsInfinity(value);
        }

        public static bool IsZero(float value)
        {
            if (IsNan(value)) return false;
            var r1 = value == 0f;
            var r2 = value == -0f;
            return r1 || r2;
        }

        public static bool IsEquals(float left, float right)
        {
            if (IsNan(left) || IsNan(right)) return false;
            return left == right;
        }

        public static bool IsLessThan(float left, float right)
        {
            if (IsNan(left) || IsNan(right)) return false;
            return left < right;
        }

        public static bool IsLessOrEqualsThan(float left, float right)
        {
            if (IsNan(left) || IsNan(right)) return false;
            return left <= right;
        }

        public static bool IsGreatOrEqualsThan(float left, float right)
        {
            if (IsNan(left) || IsNan(right)) return false;
            return left >= right;
        }

        public static float Sign2(float left, float right)
        {
            var a = left - right;
            return (0.0 < a ? 1 : 0) - (a < 0.0 ? 1 : 0);
        }
    }
}