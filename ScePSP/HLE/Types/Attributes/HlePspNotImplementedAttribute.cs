using System;

namespace ScePSP.Hle
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HlePspNotImplementedAttribute : Attribute
    {
        public bool PartialImplemented = false;

        public bool Notice = true;
    }

    public sealed class PspTestedAttribute : Attribute
    {
    }

    public sealed class PspUntestedAttribute : Attribute
    {
    }
}