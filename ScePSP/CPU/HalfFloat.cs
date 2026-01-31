using ScePSPUtils;

namespace ScePSP.Cpu
{
    public struct Float32
    {
        public uint Value;
    }

    public struct Float16
    {
        public ushort Value;

        public bool Sign { get { return BitUtils.Extract(Value, 15, 1) != 0; } }
        public uint Exponent { get { return BitUtils.Extract(Value, 10, 5); } }
        public uint Fraction { get { return BitUtils.Extract(Value, 0, 10); } }
    }

    public class HalfFloat
    {
        static public int FloatToHalfFloat(float Float)
        {
            var i = MathFloat.ReinterpretFloatAsInt(Float);
            int s = ((i >> 16) & 0x00008000);              // sign
            int e = ((i >> 23) & 0x000000ff) - (127 - 15); // exponent
            int f = ((i >> 0) & 0x007fffff);              // fraction

            // need to handle NaNs and Inf?
            if (e <= 0)
            {
                if (e < -10)
                {
                    if (s != 0)
                    {
                        // handle -0.0
                        return 0x8000;
                    }
                    return 0;
                }
                f = (f | 0x00800000) >> (1 - e);
                return s | (f >> 13);
            }
            else if (e == 0xff - (127 - 15))
            {
                if (f == 0)
                {
                    // Inf
                    return s | 0x7c00;
                }
                // NAN
                f >>= 13;
                return s | 0x7c00 | f | ((f == 0) ? 1 : 0);
            }
            if (e > 30)
            {
                // Overflow
                return s | 0x7c00;
            }
            return s | (e << 10) | (f >> 13);
        }

        public static float ToFloat(int imm16)
        {
            int s = (imm16 >> 15) & 0x00000001; // Sign
            int e = (imm16 >> 10) & 0x0000001f; // Exponent
            int f = (imm16 >> 0) & 0x000003ff;  // Fraction

            // Need to handle 0x7C00 INF and 0xFC00 -INF?
            if (e == 0)
            {
                // Need to handle +-0 case f==0 or f=0x8000?
                if (f == 0)
                {
                    // Plus or minus zero
                    return MathFloat.ReinterpretIntAsFloat(s << 31);
                }
                // Denormalized number -- renormalize it
                while ((f & 0x00000400) == 0)
                {
                    f <<= 1;
                    e -= 1;
                }
                e += 1;
                f &= ~0x00000400;
            }
            else if (e == 31)
            {
                if (f == 0)
                {
                    // Inf
                    return MathFloat.ReinterpretIntAsFloat((s << 31) | 0x7f800000);
                }
                // NaN
                return MathFloat.ReinterpretIntAsFloat((s << 31) | 0x7f800000 | (f << 13));
            }

            e = e + (127 - 15);
            f = f << 13;

            return MathFloat.ReinterpretIntAsFloat((s << 31) | (e << 23) | f);
        }
    }
}
