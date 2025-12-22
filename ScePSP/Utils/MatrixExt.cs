using System.Numerics;

namespace ScePSP.Utils
{
    public static class MatrixExt
    {
        public static Matrix4x4 Transpose(this Matrix4x4 that) => Matrix4x4.Transpose(that);
    }
}