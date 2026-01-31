using ScePSP.Cpu;
using System;

namespace ScePSP.Hle
{
    public struct HleFunctionEntry
    {
        public uint NID;
        public String Name;
        public String Description;
        public HleModuleHost Module;
        public string ModuleName;
        public Action<CpuThreadState> Delegate;

        public override string ToString()
        {
            return String.Format("FunctionEntry(NID=0x{0:X}, Name='{1}', Description='{2}', Module='{3}')", NID, Name, Description, Module);
        }
    }
}
