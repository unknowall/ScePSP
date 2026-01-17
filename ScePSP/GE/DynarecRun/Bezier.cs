using ScePSPUtils;
using System.Numerics;

namespace ScePSP.GE.Run
{
    public unsafe sealed partial class GERunner
    {
        public void OP_BEZIER()
        {
            var UCount = Param8(0);
            var VCount = Param8(8);

            DrawBezier(UCount, VCount);
        }

        private static float[] BernsteinCoeff(float u)
        {
            float uPow2 = u * u;
            float uPow3 = uPow2 * u;
            float u1 = 1 - u;
            float u1Pow2 = u1 * u1;
            float u1Pow3 = u1Pow2 * u1;

            return new float[] {
                u1Pow3,
                3 * u * u1Pow2,
                3 * uPow2 * u1,
                uPow3,
            };
        }

        private static void PointMultAdd(ref VertexInfo dest, ref VertexInfo src, float f)
        {
            dest.Position += src.Position * f;
            dest.Texture += src.Texture * f;
            dest.Color += src.Color * f;
            dest.Normal += src.Normal * f;
        }

        private VertexInfo[,] GetControlPoints(int UCount, int VCount)
        {
            var ControlPoints = new VertexInfo[UCount, VCount];

            var VertexPtr = (byte*)GECore.GeList.Memory.PspAddressToPointerSafe(GpuState->GetAddressRelativeToBaseOffset(GpuState->VertexAddress), 0);
            var VertexReader = new VertexReader();
            VertexReader.SetVertexTypeStruct(GpuState->VertexState.Type, VertexPtr);

            for (int u = 0; u < UCount; u++)
            {
                for (int v = 0; v < VCount; v++)
                {
                    ControlPoints[u, v] = VertexReader.ReadVertex(v * UCount + u);
                }
            }
            return ControlPoints;
        }

        private void DrawBezier(int uCount, int vCount)
        {
            var divS = GECore.GEStateStruct->PatchState.DivS;
            var divT = GECore.GEStateStruct->PatchState.DivT;

            if ((uCount - 1) % 3 != 0 || (vCount - 1) % 3 != 0)
            {
                Logger.Warning("Unsupported bezier parameters ucount=" + uCount + " vcount=" + vCount);
                return;
            }
            if (divS <= 0 || divT <= 0)
            {
                Logger.Warning("Unsupported bezier patches patch_div_s=" + divS + " patch_div_t=" + divT);
                return;
            }

            var anchors = GetControlPoints(uCount, vCount);

            var patch = new VertexInfo[divS + 1, divT + 1];

            var upcount = uCount / 3;
            var vpcount = vCount / 3;

            var ucoeff = new float[divS + 1][];

            for (var j = 0; j <= divT; j++)
            {
                var vglobal = (float)j * vpcount / divT;

                var vpatch = (int)vglobal;
                var v = vglobal - vpatch;
                if (j == divT)
                {
                    vpatch--;
                    v = 1.0f;
                }
                var vcoeff = BernsteinCoeff(v);

                for (var i = 0; i <= divS; i++)
                {
                    var uglobal = (float)i * upcount / divS;
                    var upatch = (int)uglobal;
                    var u = uglobal - upatch;
                    if (i == divS)
                    {
                        upatch--;
                        u = 1.0f;
                    }
                    ucoeff[i] = BernsteinCoeff(u);

                    var p = default(VertexInfo);
                    p.Position = Vector4.Zero;
                    p.Normal = Vector4.Zero;

                    for (var ii = 0; ii < 4; ++ii)
                    {
                        for (var jj = 0; jj < 4; ++jj)
                        {
                            PointMultAdd(
                                ref p,
                                ref anchors[3 * upatch + ii, 3 * vpatch + jj],
                                ucoeff[i][ii] * vcoeff[jj]
                            );
                        }
                    }

                    p.Texture.X = uglobal;
                    p.Texture.Y = vglobal;

                    patch[i, j] = p;
                }
            }

            GECore.GeList.BackEnd.BeforeDraw(GECore.GEStateStruct);
            GECore.GeList.BackEnd.DrawCurvedSurface(GlobalGpuState, GECore.GEStateStruct, patch, uCount, vCount);
        }
    }
}
