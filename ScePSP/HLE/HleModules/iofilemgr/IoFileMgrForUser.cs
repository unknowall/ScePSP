using ScePSP.Hle.Attributes;
using ScePSP.Hle.Managers;
using ScePSP.Hle.Vfs;
using ScePSPUtils;

namespace ScePSP.Hle.Modules.iofilemgr
{
    [HlePspModule(ModuleFlags = ModuleFlags.UserMode | ModuleFlags.Flags0x00010011)]
    public partial class IoFileMgrForUser : HleModuleHost
    {
        static Logger Logger = Logger.GetLogger("IoFileMgrForUser");

        protected HleIoManager HleIoManager => PSPDrivers.HLE.HleIoManager;

        public HleIoDrvFileArg GetFileArgFromHandle(SceUID FileHandle)
        {
            return HleIoManager.HleIoDrvFileArgPool.Get(FileHandle);
        }

        public override void Dispose()
        {
            HleIoManager.HleIoDrvFileArgPool.RemoveAll();
            base.Dispose();
        }
    }
}