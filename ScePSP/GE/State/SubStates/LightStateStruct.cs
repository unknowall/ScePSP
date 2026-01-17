using System.Numerics;
using System.Runtime.InteropServices;
namespace ScePSP.GE.State.SubStates
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AttenuationStruct
    {
        public float Constant;
        public float Linear;
        public float Quadratic;
    }

    public struct Vector4fRef
    {
        public float X, Y, Z, W;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LightStateStruct
    {
        public bool Enabled;

        public LightTypeEnum Type;

        public LightModelEnum Kind;

        public Vector4 Position;

        public Vector4 SpotDirection;

        public AttenuationStruct Attenuation;

        public float SpotExponent;

        public float SpotCutoff;

        public ColorfStruct AmbientColor;

        public ColorfStruct DiffuseColor;

        public ColorfStruct SpecularColor;
    }
}
