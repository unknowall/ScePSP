using SafeILGenerator.Utils;
using System;

namespace ScePSP.Core.Cpu
{
    public class NativeSyscallInfo
    {
        public string Name => $"{ModuleImportName}.{FunctionEntryName} (0x{Nid:X8})";

        public IlInstanceHolderPoolItem<Action<CpuThreadState>> PoolItem;
        public uint Nid;
        public string FunctionEntryName;
        public string ModuleImportName;
    }
}