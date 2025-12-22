using ScePSP.Hle.Attributes;

namespace ScePSP.Hle.Modules.sysmem
{
    [HlePspModule(ModuleFlags = ModuleFlags.UserMode | ModuleFlags.Flags0x00000011)]
    public class SysMemForKernel : SysMemUserForUser
    {
    }
}