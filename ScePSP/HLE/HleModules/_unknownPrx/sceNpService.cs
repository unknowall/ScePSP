using ScePSP.Hle.Attributes;

namespace ScePSP.Hle.Modules._unknownPrx
{
    [HlePspModule(ModuleFlags = ModuleFlags.KernelMode | ModuleFlags.Flags0x00010011)]
    public class sceNpService : HleModuleHost
    {
    }
}