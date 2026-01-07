using ScePSP.cheats;
using ScePSP.Core;
using ScePSP.Core.AudioBackEnd;
using ScePSP.Core.AudioBackEnd.Null;
using ScePSP.Core.AudioBackEnd.SDL;
using ScePSP.Core.Components.Battery;
using ScePSP.Core.Components.Controller;
using ScePSP.Core.Components.Crypto;
using ScePSP.Core.Components.Display;
using ScePSP.Core.Components.Rtc;
using ScePSP.Core.Cpu;
using ScePSP.Core.Cpu.Dynarec;
using ScePSP.Core.Cpu.InstructionCache;
using ScePSP.Core.GpuBackEnd;
using ScePSP.Core.GpuBackEnd.Null;
using ScePSP.Core.GpuBackEnd.OpenGL;
using ScePSP.Core.GpuBackEnd.Soft;
using ScePSP.Core.Memory;
using ScePSP.Hle;
using ScePSP.Hle.Formats;
using ScePSP.Hle.Interop;
using ScePSP.Hle.Loader;
using ScePSP.Hle.Managers;
using ScePSP.Hle.Modules;
using ScePSP.Hle.Modules.audio;
using ScePSP.Hle.Modules.display;
using ScePSP.Hle.Modules.interruptman;
using ScePSP.Hle.Modules.iofilemgr;
using ScePSP.Hle.Modules.modulemgr;
using ScePSP.Hle.Modules.pspnet;
using ScePSP.Hle.Modules.sysmem;
using ScePSP.Hle.Modules.threadman;
using ScePSP.Hle.Vfs;
using ScePSP.Hle.Vfs.Emulator;
using ScePSP.Runner;
using ScePSP.Runner.Tasks.Audio;
using ScePSP.Runner.Tasks.Cpu;
using ScePSP.Runner.Tasks.Display;
using ScePSP.Runner.Tasks.Gpu;
using ScePSP.TextureHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static ScePSP.Hle.Modules.iofilemgr.IoFileMgrForUser;
using static ScePSP.Hle.Modules.pspnet.sceNetAdhocMatching;

namespace ScePSP
{
    public static class PSPDrivers
    {
        public static bool Inited = false;

        public static PspRunner Runner;

        public static CpuProcessor CPU;

        public static ICpuConnector CpuConnector;

        public static IGpuConnector GpuConnector;

        public static IInterruptManager InterruptManager;

        public static MethodCache MethodCache;

        public static DynarecFunctionCompiler DynarecFunctionCompiler;

        public static GpuProcessor GE;

        public static PspAudio PspAudio;

        public static PspMemory PspMemory;

        public static PspRtc PspRtc;

        public static GpuBackEnd GpuBackEnd;

        public static AudioBackEnd AudioBackEnd;

        public static PspDisplay PspDisplay;

        public static CWCheatPlugin CWCheatPlugin;

        public static TextureHookPlugin TextureHookPlugin;

        public static ElfPspLoader Loader;

        public static class GameInfo
        {
            public static string FileName;

            public static string Title;

            public static string ID;

            public static bool IsIso;

            public static Psf Psf;
        }

        public static class Tasks
        {
            public static CpuTask CpuTask;

            public static GpuTask GpuTask;

            public static AudioTask AudioTask;

            public static DisplayTask DisplayTask;
        }

        public static class HLE
        {
            public static ModuleMgrForUser ModuleMgrForUser;

            public static HleUidPoolManager HleUidPoolManager;

            public static HleModuleManager HleModuleManager;

            public static HleThreadManager HleThreadManager;

            public static HleInterruptManager HleInterruptManager;

            public static HleMemoryManager MemoryManager;

            public static HleCallbackManager HleCallbackManager;

            public static HleInterop HleInterop;

            public static HleIoManager HleIoManager;

            public static IoFileMgrForUser IoFileMgrForUser;

            public static ThreadManForUser ThreadManForUser;

            public static HleIoDriverEmulator HleIoDriverEmulator;

            public static HleIoDriverMountable MemoryStickMountable;

            public static HleRegistryManager HleRegistryManager;

            public static HleSemaphoreManager SemaphoreManager;

            public static HleOutputHandler HleOutputHandler;
        }

        public static List<HleModuleHost> HleModuleHostList = new List<HleModuleHost>();

        public static List<HleModuleGuest> HleModuleGuestList = new List<HleModuleGuest>();

        public static List<Matching> MatchingList = new List<Matching>();

        public static List<VirtualTimer> VirtualTimerList = new List<VirtualTimer>();

        public static List<GuestHleIoDriver> GuestHleIoDriverList = new List<GuestHleIoDriver>();

        public static List<ElfLoader> ElfLoaderList = new List<ElfLoader>();

        public static class HleModules
        {
            public static sceDisplay sceDisplay;

            public static sceAudio sceAudio;

            public static SysMemUserForUser SysMemUserForUser;

            public static sceNetAdhocctl sceNetAdhocctl;

            public static sceNet sceNet;

            public static KDebugForKernel KDebugForKernel;
        }

        public static class Devices
        {
            public static Battery PspBattery;

            public static Kirk Kirk;

            public static PspDisplay Display;

            public static PspController PspController;
        }

        public static class Config
        {
            public static PspStoredConfig StoredConfig;

            public static CpuConfig CpuConfig;

            public static GpuConfig GpuConfig;

            public static ElfConfig ElfConfig;

            public static HleConfig HleConfig;

            public static DisplayConfig DisplayConfig;

            public static PspHleRunningConfig PspHleRunningConfig;
        }

        public enum PspGpuType
        {
            OpenGL,
            Soft,
            Null
        }

        public enum PspAudioType
        {
            SDL,
            Null
        }

        public static void initialize(PspGpuType gpu, PspAudioType audio)
        {
            Config.StoredConfig = new PspStoredConfig();
            Config.CpuConfig = new CpuConfig();
            Config.GpuConfig = new GpuConfig();
            Config.ElfConfig = new ElfConfig();
            Config.HleConfig = new HleConfig();
            Config.DisplayConfig = new DisplayConfig();
            Config.PspHleRunningConfig = new PspHleRunningConfig();

            Config.HleConfig.HleModulesDll = typeof(HleModulesRoot).Assembly;

            if (Config.StoredConfig.UseFastMemory)
            {
                PspMemory = new FastPspMemory(); 
            }else
            {
                PspMemory = new NormalPspMemory(); 
            }

            CPU = new CpuProcessor();

            DynarecFunctionCompiler = new DynarecFunctionCompiler();
            MethodCache = new MethodCache();

            GE = new GpuProcessor();
            PspRtc = new PspRtc();
            PspAudio = new PspAudio();
            PspDisplay = new PspDisplay();

            CWCheatPlugin = new CWCheatPlugin();
            TextureHookPlugin = new TextureHookPlugin();

            Loader = new ElfPspLoader();

            Devices.Display = new PspDisplay();
            Devices.PspBattery = new Battery();
            Devices.Kirk = new Kirk();
            Devices.PspController = new PspController();

            switch (gpu)
            {
                case PspGpuType.OpenGL:
                    GpuBackEnd = new OpenglBackEnd();
                    break;
                case PspGpuType.Soft:
                    GpuBackEnd = new SoftBackEnd();
                    break;
                case PspGpuType.Null:
                    GpuBackEnd = new NullBackEnd();
                    break;
            }

            switch (audio)
            {
                case PspAudioType.SDL:
                    AudioBackEnd = new SDLAudioBackEnd();
                    break;
                case PspAudioType.Null:
                    AudioBackEnd = new NullAudio();
                    break;
            }

            HLE.HleModuleManager = new HleModuleManager();
            HLE.HleUidPoolManager = new HleUidPoolManager();
            HLE.ModuleMgrForUser = new ModuleMgrForUser();
            HLE.HleThreadManager = new HleThreadManager();
            HLE.HleInterruptManager = new HleInterruptManager();
            HLE.MemoryManager = new HleMemoryManager();
            HLE.HleCallbackManager = new HleCallbackManager();
            HLE.HleInterop = new HleInterop();
            HLE.HleIoManager = new HleIoManager();
            HLE.IoFileMgrForUser = new IoFileMgrForUser();
            HLE.ThreadManForUser = new ThreadManForUser();
            HLE.HleIoDriverEmulator = new HleIoDriverEmulator();
            HLE.MemoryStickMountable = new HleIoDriverMountable();
            HLE.HleRegistryManager = new HleRegistryManager();
            HLE.SemaphoreManager = new HleSemaphoreManager();
            HLE.HleOutputHandler = new HleOutputHandler();

            HleModules.sceDisplay = new sceDisplay();
            HleModules.sceAudio = new sceAudio();
            HleModules.SysMemUserForUser = new SysMemUserForUser();
            HleModules.sceNetAdhocctl = new sceNetAdhocctl();
            HleModules.sceNet = new sceNet();
            HleModules.KDebugForKernel = new KDebugForKernel();

            CpuConnector = HLE.HleThreadManager;
            GpuConnector = HLE.HleThreadManager;
            InterruptManager = HLE.HleInterruptManager;

            Tasks.GpuTask = new GpuTask();
            Tasks.AudioTask = new AudioTask();
            Tasks.DisplayTask = new DisplayTask();
            Tasks.CpuTask = new CpuTask();

            Runner = new PspRunner();

            Inited = true;
        }

        public static void free()
        {
            if (!Inited) return;

            PspMemory.Dispose();
            TextureHookPlugin.Dispose();

            HLE.ModuleMgrForUser.Dispose();
            HLE.HleModuleManager.Dispose();
            HLE.IoFileMgrForUser.Dispose();
            HLE.ThreadManForUser.Dispose();

            foreach (var module in HleModuleHostList) module.Dispose();
            HleModuleHostList.Clear();
            foreach (var guestModule in HleModuleGuestList) guestModule.Dispose();
            HleModuleGuestList.Clear();
            foreach (var matching in MatchingList) matching.Dispose();
            MatchingList.Clear();
            foreach (var timer in VirtualTimerList) timer.Dispose();
            VirtualTimerList.Clear();
            //foreach (var driver in GuestHleIoDriverList) driver.Dispose();
            GuestHleIoDriverList.Clear();
            //foreach (var elfLoader in ElfLoaderList) elfLoader.Dispose();
            ElfLoaderList.Clear();

            HleModules.sceDisplay.Dispose();
            HleModules.sceAudio.Dispose();
            HleModules.SysMemUserForUser.Dispose();
            HleModules.sceNetAdhocctl.Dispose();
            HleModules.sceNet.Dispose();
            HleModules.KDebugForKernel.Dispose();

            GC.Collect();
        }
    }
}
