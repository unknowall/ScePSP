namespace ScePSP.GE.Run
{
    public unsafe sealed partial class GERunner
    {
        /**
		  * Draw bezier surface
		  *
		  * @param vtype    - Vertex type, look at sceGuDrawArray() for vertex definition
		  * @param ucount   - Number of vertices used in the U direction
		  * @param vcount   - Number of vertices used in the V direction
		  * @param indices  - Pointer to index buffer
		  * @param vertices - Pointer to vertex buffer
		**/
        //void sceGuDrawBezier(int vtype, int ucount, int vcount, const void* indices, const void* vertices);

        /**
		  * Set dividing for patches (beziers and splines)
		  *
		  * @param ulevel - Number of division on u direction
		  * @param vlevel - Number of division on v direction
		**/
        //void sceGuPatchDivide(unsigned int ulevel, unsigned int vlevel);

        //void sceGuPatchFrontFace(unsigned int a0);

        /**
		  * Set primitive for patches (beziers and splines)
		  *
		  * @param prim - Desired primitive type (GU_POINTS | GU_LINE_STRIP | GU_TRIANGLE_STRIP)
		**/
        //void sceGuPatchPrim(int prim);

        //void sceGuDrawSpline(int vtype, int ucount, int vcount, int uedge, int vedge, const void* indices, const void* vertices);

        public void OP_PSUB()
        {
            GpuState->PatchState.DivS = Param8(0);
            GpuState->PatchState.DivT = Param8(8);
        }

        public void OP_PFACE()
        {
            GpuState->PatchCullingState.FaceFlag = (Params24 != 0);
        }

        public void OP_SPLINE()
        {
            var sp_ucount = (int)Extract(0, 8);
            var sp_vcount = (int)Extract(8, 8);
            var sp_utype = (int)Extract(16, 2);
            var sp_vtype = (int)Extract(18, 2);

            //Console.Out.WriteLineColored(ConsoleColor.Green, "OP_SPLINE %d, %d, %d, %d", sp_ucount, sp_vcount, sp_utype, sp_vtype);

            DrawSpline(sp_ucount, sp_vcount, sp_utype, sp_vtype);
        }

        private static float[] LinearCoeff(float t)
        {
            return new[]
            {
                1 - t, // 第一个控制点权重
                t      // 第二个控制点权重
            };
        }

        internal void DrawSpline(int sp_ucount, int sp_vcount, int sp_utype, int sp_vtype)
        {
            var divS = GECore.GEStateStruct->PatchState.DivS;
            var divT = GECore.GEStateStruct->PatchState.DivT;

            if (sp_ucount <= 0 || sp_vcount <= 0 || divS <= 0 || divT <= 0)
                return;

            int normalizedUType = sp_utype == 0 ? 0 : 1;
            int normalizedVType = sp_vtype == 0 ? 0 : 1;
            bool isUCubic = normalizedUType == 1;
            bool isVCubic = normalizedVType == 1;

            var controlPoints = GetControlPoints(sp_ucount, sp_vcount);
            var splinePatch = new VertexInfo[divS + 2, divT + 2];

            int uControlStep = isUCubic ? 3 : 1;
            int vControlStep = isVCubic ? 3 : 1;

            for (int j = 0; j <= divT + 1; j++)
            {
                float vGlobal = (float)j * (divT) / divT;
                int vPatch = (int)vGlobal;
                float v = vGlobal - vPatch;
                float[] vCoeff = isVCubic ? BernsteinCoeff(v) : LinearCoeff(v);

                for (int i = 0; i <= divS + 1; i++)
                {
                    float uGlobal = (float)i * (divS) / divS;
                    int uPatch = (int)uGlobal;
                    float u = uGlobal - uPatch;
                    float[] uCoeff = isUCubic ? BernsteinCoeff(u) : LinearCoeff(u);

                    var currentVertex = default(VertexInfo);
                    int uDim = isUCubic ? 4 : 2;
                    int vDim = isVCubic ? 4 : 2;

                    for (int ui = 0; ui < uDim; ui++)
                    {
                        for (int vi = 0; vi < vDim; vi++)
                        {
                            int uIndex = uPatch + ui;
                            int vIndex = vPatch + vi;
                            if (uIndex >= sp_ucount || vIndex >= sp_vcount)
                                continue;

                            PointMultAdd(ref currentVertex, ref controlPoints[uIndex, vIndex], uCoeff[ui] * vCoeff[vi]);
                        }
                    }

                    currentVertex.Texture.X = uGlobal;
                    currentVertex.Texture.Y = vGlobal;
                    splinePatch[i, j] = currentVertex;
                }
            }

            GECore.GeList.BackEnd.BeforeDraw(GECore.GEStateStruct);
            GECore.GeList.BackEnd.DrawSpline(GlobalGpuState, GECore.GEStateStruct, splinePatch, sp_ucount, sp_vcount, sp_utype, sp_vtype, normalizedUType, normalizedVType);
        }

    }
}
