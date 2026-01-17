using System.Runtime.InteropServices;

namespace ScePSP.GE.State.SubStates
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SkinningStateStruct
    {
        public int CurrentBoneIndex;

        public GpuMatrix4x3Struct BoneMatrix(int idx)
        {
            switch (idx)
            {
                case 0: return BoneMatrix0;
                case 1: return BoneMatrix1;
                case 2: return BoneMatrix2;
                case 3: return BoneMatrix3;
                case 4: return BoneMatrix4;
                case 5: return BoneMatrix5;
                case 6: return BoneMatrix6;
                case 7: return BoneMatrix7;
            }

            return BoneMatrix0;
        }

        public GpuMatrix4x3Struct BoneMatrix0, BoneMatrix1, BoneMatrix2, BoneMatrix3, BoneMatrix4, BoneMatrix5, BoneMatrix6, BoneMatrix7;
    }
}
