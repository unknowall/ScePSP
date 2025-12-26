using System.Numerics;
using System.Runtime.InteropServices;

namespace ScePSP.Core.Gpu
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct VertexInfoWeights
    {
        public fixed float W[8];

        public VertexInfoWeights(VertexInfo vertexInfo)
        {
            for (var n = 0; n < 8; n++) W[n] = vertexInfo.Weights[n];
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct VertexInfo
    {
        public fixed float Weights[8];
        public Vector4 Texture;
        public Vector4 Color;
        public Vector4 Normal;
        public Vector4 Position;

        public override string ToString() => $"VertexInfo(Position={Position}, Normal={Normal}, UV={Texture}, COLOR={Color})";
    }
}