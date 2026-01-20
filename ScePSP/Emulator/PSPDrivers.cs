using ScePSP.cheats;
using ScePSP.BackEnd.NullAudio;
using ScePSP.BackEnd.SDL;
using ScePSP.Devices.Battery;
using ScePSP.Devices.Controller;
using ScePSP.Devices.Crypto;
using ScePSP.Devices.Display;
using ScePSP.Devices.Rtc;
using ScePSP.Cpu;
using ScePSP.Cpu.Dynarec;
using ScePSP.Cpu.InstructionCache;
using ScePSP.BackEnd;
using ScePSP.BackEnd.OpenGL;
using ScePSP.Memory;
using ScePSP.GE;
using ScePSP.Hle;
using ScePSP.Hle.Formats;
using ScePSP.Hle.Interop;
using ScePSP.Hle.Loader;
using ScePSP.Hle.Managers;
using ScePSP.Hle.Modules;
using ScePSP.Hle.Modules.audio;
using ScePSP.Hle.Modules.display;
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
using ScePSP.Runner.Tasks.GE;
using System;
using System.Collections.Generic;
using static ScePSP.Hle.Modules.iofilemgr.IoFileMgrForUser;
using static ScePSP.Hle.Modules.pspnet.sceNetAdhocMatching;

namespace ScePSP
{
    public static class PSPDrivers
    {
        public static bool Inited = false;

        public static bool Runing = false;

        public static Runner.Runner Runner;

        public static CpuProcessor CPU;

        public static ICpuConnector CpuConnector;

        public static IGEConnector GEConnector;

        public static IInterruptManager InterruptManager;

        public static MethodCache MethodCache;

        public static DynarecFunctionCompiler DynarecFunctionCompiler;

        public static GEList GEList;

        public static PspAudio PspAudio;

        public static PspMemory PspMemory;

        public static PspRtc PspRtc;

        public static GEBackEnd GeBackEnd;

        public static AudioBackEnd AudioBackEnd;

        public static PspDisplay PspDisplay;

        public static CWCheatList CWCheatList;

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

            public static GETask GpuTask;

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

            public static PspController Controller;
        }

        public static class Config
        {
            public static PspStoredConfig StoredConfig;

            public static CpuConfig CpuConfig;

            public static ElfConfig ElfConfig;

            public static HleConfig HleConfig;

            public static DisplayConfig DisplayConfig;

            public static PspHleRunningConfig PspHleRunningConfig;
        }

        public enum PspGpuType
        {
            OpenGL,
            Null
        }

        public enum PspAudioType
        {
            SDL,
            Null
        }

        public static void initialize(PspGpuType gpu, PspAudioType audio, IntPtr WindowHandle = default)
        {
            Config.StoredConfig = PspStoredConfig.Load();
            Config.CpuConfig = new CpuConfig();
            Config.ElfConfig = new ElfConfig();
            Config.HleConfig = new HleConfig();
            Config.DisplayConfig = new DisplayConfig();
            Config.PspHleRunningConfig = new PspHleRunningConfig();

            Config.DisplayConfig.WindowHandle = WindowHandle;

            Config.HleConfig.HleModulesDll = typeof(HleModulesRoot).Assembly;

            if (Config.StoredConfig.UseFastMemory)
            {
                PspMemory = new FastPspMemory();
            }
            else
            {
                PspMemory = new NormalPspMemory();
            }

            CPU = new CpuProcessor();

            DynarecFunctionCompiler = new DynarecFunctionCompiler();
            MethodCache = new MethodCache();

            GEList = new GEList();
            PspRtc = new PspRtc();
            PspAudio = new PspAudio();
            PspDisplay = new PspDisplay();

            CWCheatList = new CWCheatList();

            Loader = new ElfPspLoader();

            Devices.PspBattery = new Battery();
            Devices.Kirk = new Kirk();
            Devices.Controller = new PspController();

            switch (gpu)
            {
                case PspGpuType.OpenGL:
                    GeBackEnd = new GLBackEnd();
                    break;
                    break;
                case PspGpuType.Null:
                    GeBackEnd = new NullRender();
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
            GEConnector = HLE.HleThreadManager;
            InterruptManager = HLE.HleInterruptManager;

            Tasks.CpuTask = new CpuTask();
            Tasks.GpuTask = new GETask();
            Tasks.AudioTask = new AudioTask();
            Tasks.DisplayTask = new DisplayTask();

            Tasks.DisplayTask.type = gpu;

            Runner = new Runner.Runner();

            Runing = true;

            Inited = true;
        }

        public static void free()
        {
            if (!Inited) return;

            Runing = false;

            Tasks.CpuTask.Running = false;
            Tasks.GpuTask.Running = false;
            Tasks.AudioTask.Running = false;
            Tasks.DisplayTask.Running = false;

            PspMemory.Dispose();

            MethodCache.Runing = false;

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

            Tasks.DisplayTask.Dispose();

            GC.Collect();
        }
    }
}
