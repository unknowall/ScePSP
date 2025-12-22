using ScePSP.Hle.Attributes;

namespace ScePSP.Hle.Modules.iofilemgr
{
    [HlePspModule(ModuleFlags = ModuleFlags.KernelMode | ModuleFlags.Flags0x00010011)]
    public class IoFileMgrForKernel : IoFileMgrForUser
    {
    }
}