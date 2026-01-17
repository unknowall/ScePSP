using SafeILGenerator.Utils;
using System;

namespace ScePSP.Cpu
{
    public class NativeSyscallInfo
    {
        public string Name { get { return String.Format("{0}.{1} (0x{2:X8})", ModuleImportName, FunctionEntryName, NID); } }
        public ILInstanceHolderPoolItem<Action<CpuThreadState>> PoolItem;
        public uint NID;
        public string FunctionEntryName;
        public string ModuleImportName;
    }
}
