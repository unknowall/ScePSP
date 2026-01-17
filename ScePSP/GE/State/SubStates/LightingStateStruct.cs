using System.Runtime.InteropServices;

namespace ScePSP.GE.State.SubStates
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LightingStateStruct
    {
        public bool Enabled;

        public ColorfStruct AmbientModelColor;

        public ColorfStruct DiffuseModelColor;

        public ColorfStruct SpecularModelColor;

        public ColorfStruct EmissiveModelColor;

        public ColorfStruct AmbientLightColor;

        public float SpecularPower;

        public LightStateStruct Light(int idx)
        {
            switch (idx)
            {
                case 0: return Light0;
                case 1: return Light1;
                case 2: return Light2;
                case 3: return Light3;
            }

            return Light0;
        }

        public LightStateStruct Light0, Light1, Light2, Light3;

        public LightComponentsSet MaterialColorComponents;

        public LightModelEnum LightModel;
    }
}
