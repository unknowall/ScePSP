using ScePSP.Hle.Attributes;
using ScePSP.Hle.Managers;
using ScePSP.Hle.Modules.modulemgr;

namespace ScePSP.Hle.Modules._unknownPrx
{
    [HlePspModule(ModuleFlags = ModuleFlags.KernelMode | ModuleFlags.Flags0x00010011)]
    public unsafe class ModuleMgrForKernel : ModuleMgrForUser
    {
        HleModuleManager ModuleManager => PSPDrivers.HLE.HleModuleManager;

        [HlePspFunction(NID = 0xA1A78C58, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceKernelLoadModuleForLoadExecVSHDisc(string FileName, uint Flags, ModuleMgrForUser.SceKernelLMOption* option)
        {
            return sceKernelLoadModule(FileName, Flags, option);
        }
    }
}