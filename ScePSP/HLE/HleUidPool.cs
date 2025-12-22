using System;

namespace ScePSP.Hle.Managers
{
    public class HleUidPool<TType> : HleUidPoolSpecial<TType, int> where TType : IDisposable
    {
    }
}