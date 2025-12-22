using ScePSP.Hle.Attributes;

namespace ScePSP.Hle.Modules.threadman
{
    [HlePspModule(ModuleFlags = ModuleFlags.UserMode | ModuleFlags.Flags0x00010011)]
    public class ThreadManForKernel : ThreadManForUser
    {
    }
}