using ScePSP.Hle.Attributes;

namespace ScePSP.Hle.Modules.mpeg
{
    [HlePspModule(ModuleFlags = ModuleFlags.KernelMode | ModuleFlags.Flags0x00010011)]
    public class sceMpegbase : HleModuleHost
    {
    }
}