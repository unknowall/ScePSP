using System;

namespace ScePSP.Hle
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HleTrackCallAttribute : Attribute
    {
        public bool PartialImplemented = false;

        public bool Notice = true; //Debug Set True
    }

    public sealed class PspTestedAttribute : Attribute
    {
    }

    public sealed class PspUntestedAttribute : Attribute
    {
    }
}