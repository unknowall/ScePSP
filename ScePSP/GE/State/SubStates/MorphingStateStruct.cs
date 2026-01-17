using System.Runtime.InteropServices;

namespace ScePSP.GE.State.SubStates
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MorphingStateStruct
    {
        public float MorphWeight0;

        public float MorphWeight1;

        public float MorphWeight2;

        public float MorphWeight3;

        public float MorphWeight4;

        public float MorphWeight5;

        public float MorphWeight6;

        public float MorphWeight7;

        public float MorphWeight(int idx)
        {
            switch (idx)
            {
                case 0:return MorphWeight0;
                case 1:return MorphWeight1;
                case 2:return MorphWeight2;
                case 3:return MorphWeight3;
                case 4:return MorphWeight4;
                case 5:return MorphWeight5;
                case 6:return MorphWeight6;
                case 7:return MorphWeight7;
            }
            return MorphWeight0;
        }
    }
}
