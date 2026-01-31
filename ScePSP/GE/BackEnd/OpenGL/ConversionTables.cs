using LightGL;

namespace ScePSP.BackEnd.OpenGL
{
    internal static class ConversionTables
    {
        internal static readonly int[] StencilOperationTranslate =
        {
            GL.GL_KEEP,
            GL.GL_ZERO,
            GL.GL_REPLACE,
            GL.GL_INVERT,
            GL.GL_INCR,
            GL.GL_DECR,
        };

        internal static readonly int[] StencilFunctionTranslate =
        {
            GL.GL_NEVER,
            GL.GL_ALWAYS,
            GL.GL_EQUAL,
            GL.GL_NOTEQUAL,
            GL.GL_LESS,
            GL.GL_LEQUAL,
            GL.GL_GREATER,
            GL.GL_GEQUAL,
        };

        internal static readonly int[] DepthFunctionTranslate =
        {
            GL.GL_NEVER,
            GL.GL_ALWAYS,
            GL.GL_EQUAL,
            GL.GL_NOTEQUAL,
            GL.GL_LESS,
            GL.GL_LEQUAL,
            GL.GL_GREATER,
            GL.GL_GEQUAL,
        };

        internal static readonly int[] BlendEquationTranslate =
        {
            GL.GL_FUNC_ADD,                 // 0 GU_ADD
            GL.GL_FUNC_SUBTRACT,            // 1 GU_SUBTRACT
            GL.GL_FUNC_REVERSE_SUBTRACT,    // 2 GU_REVERSE_SUBTRACT
            GL.GL_MIN,                      // 3 GU_MIN
            GL.GL_MAX,                      // 4 GU_MAX
            GL.GL_FUNC_ADD,                 // 5 GU_ABS (fallback)
        };

        internal static readonly int[] BlendFuncTranslate =
        {
            GL.GL_SRC_COLOR,               // 0 GU_OTHER_COLOR src: dstColor, dst: srcColor
            GL.GL_ONE_MINUS_SRC_COLOR,     // 1 GU_ONE_MINUS_SRC_COLOR
            GL.GL_SRC_ALPHA,               // 2 GU_SRC_ALPHA
            GL.GL_ONE_MINUS_SRC_ALPHA,     // 3 GU_ONE_MINUS_SRC_ALPHA
            GL.GL_DST_ALPHA,               // 4 GU_DST_ALPHA
            GL.GL_ONE_MINUS_DST_ALPHA,     // 5 GU_ONE_MINUS_DST_ALPHA
            GL.GL_SRC1_ALPHA,              // 6 GU_DOUBLE_SRC_ALPHA
            GL.GL_ONE_MINUS_SRC1_ALPHA,    // 7 GU_ONE_MINUS_DOUBLE_SRC_ALPHA
            GL.GL_DST_ALPHA,               // 8 GU_DOUBLE_DST_ALPHA
            GL.GL_ONE_MINUS_DST_ALPHA,     // 9 GU_ONE_MINUS_DOUBLE_DST_ALPHA
            GL.GL_CONSTANT_COLOR,          // 10 GU_FIX
        };

        internal static readonly int[] BlendFuncSrcTranslate =
{
            GL.GL_SRC_COLOR, // 0 GU_SRC_COLOR,
            GL.GL_ONE_MINUS_SRC_COLOR, // 1 GU_ONE_MINUS_SRC_COLOR,
            GL.GL_SRC_ALPHA, // 2 GU_SRC_ALPHA,
            GL.GL_ONE_MINUS_SRC_ALPHA, // 3 GU_ONE_MINUS_SRC_ALPHA,
            GL.GL_DST_ALPHA, // 4 -,
            GL.GL_ONE_MINUS_DST_ALPHA, // 5 -,
            GL.GL_SRC_ALPHA, // 6 -,
            GL.GL_ONE_MINUS_SRC_ALPHA, // 7 -,
            GL.GL_DST_ALPHA, // 8 -,
            GL.GL_ONE_MINUS_DST_ALPHA, // 9 -,
            GL.GL_SRC_ALPHA, // 10 GU_FIX
        };

        internal static readonly int[] BlendFuncDstTranslate =
        {
            GL.GL_SRC_COLOR,               // 0 GU_DST_COLOR
            GL.GL_ONE_MINUS_SRC_COLOR,     // 1 GU_ONE_MINUS_DST_COLOR
            GL.GL_DST_ALPHA,               // 2 
            GL.GL_ONE_MINUS_DST_ALPHA,     // 3 
            GL.GL_DST_ALPHA,               // 4 GU_DST_ALPHA
            GL.GL_ONE_MINUS_DST_ALPHA,     // 5 GU_ONE_MINUS_DST_ALPHA
            GL.GL_DST_ALPHA,               // 6 
            GL.GL_ONE_MINUS_DST_ALPHA,     // 7 
            GL.GL_DST_ALPHA,               // 8
            GL.GL_ONE_MINUS_DST_ALPHA,     // 9
            GL.GL_ONE_MINUS_CONSTANT_COLOR,// 10 GU_FIX
        };

    }
}