using ScePSP.Components.Display;
using ScePSP.Core;
using ScePSP.Cpu;
using ScePSP.Cpu.Assembler;
using ScePSP.Hle;
using ScePSP.Hle.Formats;
using ScePSP.Hle.Formats.Archive;
using ScePSP.Hle.Loader;
using ScePSP.Hle.Managers;
using ScePSP.Hle.Modules.ctrl;
using ScePSP.Hle.Modules.display;
using ScePSP.Hle.Modules.emulator;
using ScePSP.Hle.Modules.loadexec;
using ScePSP.Hle.Modules.threadman;
using ScePSP.Hle.Modules.utils;
using ScePSP.Hle.Vfs;
using ScePSP.Hle.Vfs.Emulator;
using ScePSP.Hle.Vfs.Iso;
using ScePSP.Hle.Vfs.Local;
using ScePSP.Hle.Vfs.MemoryStick;
using ScePSP.Hle.Vfs.Zip;
using ScePSP.Memory;
using ScePSP.Rtc;
using ScePSPUtils;
using ScePSPUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ScePSP.Runner.Cpu
{
    public unsafe sealed class CpuThread : DeviceThread, IContextInitialize
    {
        static Logger Logger = Logger.GetLogger("CpuThread");

        protected override string ThreadName { get { return "CpuThread"; } }

        [Context]
        public CpuProcessor CpuProcessor;

        [Context]
        public PspRtc PspRtc;

        [Context]
        public HleThreadManager HleThreadManager;

        [Context]
        public PspMemory PspMemory;

        [Context]
        public ElfPspLoader Loader;

        [Context]
        public HleMemoryManager MemoryManager;

        [Context]
        public HleCallbackManager HleCallbackManager;

        [Context]
        public HleModuleManager ModuleManager;

        [Context]
        public HleIoManager HleIoManager;

        [Context]
        public ThreadManForUser ThreadManForUser;

        [Context]
        HleIoDriverEmulator HleIoDriverEmulator;

        [Context]
        ElfConfig ElfConfig;

        [Context]
        HleConfig HleConfig;

        [Context]
        public DisplayConfig DisplayConfig;

        public HleIoDriverMountable MemoryStickMountable;

        public AutoResetEvent StoppedEndedEvent = new AutoResetEvent(false);

        void IContextInitialize.Initialize()
        {
            RegisterDevices();
        }

        void RegisterDevices()
        {
            string MemoryStickRootFolder = ApplicationPaths.MemoryStickRootFolder;

            try { Directory.CreateDirectory(MemoryStickRootFolder); }
            catch { }

            MemoryStickMountable = new HleIoDriverMountable();
            MemoryStickMountable.Mount("/", new HleIoDriverLocalFileSystem(MemoryStickRootFolder));
            var MemoryStick = new HleIoDriverMemoryStick(PspMemory, HleCallbackManager, MemoryStickMountable);

            // http://forums.ps2dev.org/viewtopic.php?t=5680
            HleIoManager.SetDriver("host:", MemoryStick);
            HleIoManager.SetDriver("ms:", MemoryStick);
            HleIoManager.SetDriver("fatms:", MemoryStick);
            HleIoManager.SetDriver("fatmsOem:", MemoryStick);
            HleIoManager.SetDriver("mscmhc:", MemoryStick);

            HleIoManager.SetDriver("msstor:", new ReadonlyHleIoDriver(MemoryStick));
            HleIoManager.SetDriver("msstor0p:", new ReadonlyHleIoDriver(MemoryStick));

            HleIoManager.SetDriver("disc:", MemoryStick);
            HleIoManager.SetDriver("umd:", MemoryStick);

            HleIoManager.SetDriver("emulator:", HleIoDriverEmulator);
            HleIoManager.SetDriver("kemulator:", HleIoDriverEmulator);

            HleIoManager.SetDriver("flash:", new HleIoDriverZip(new ZipArchive(ApplicationPaths.AssertFolder + "/flash0.zip")));
        }

        public IsoFile SetIso(string IsoFile)
        {
            var Iso = IsoLoader.GetIso(IsoFile);
            var Umd = new HleIoDriverIso(Iso);
            HleIoManager.SetDriver("disc:", Umd);
            HleIoManager.SetDriver("umd:", Umd);
            //HleIoManager.SetDriver("host:", Umd);
            HleIoManager.SetDriver(":", Umd);
            HleIoManager.Chdir("disc0:/PSP_GAME/USRDIR");
            return Iso;
        }

        void SetVirtualFolder(string VirtualDirectory)
        {
            MemoryStickMountable.Mount(
                "/PSP/GAME/virtual",
                new HleIoDriverLocalFileSystem(VirtualDirectory)
            //.AsReadonlyHleIoDriver()
            );
        }

        void RegisterSyscalls()
        {
            new MipsAssembler(new PspMemoryStream(PspMemory)).Assemble(
                @"
					.code CODE_PTR_EXIT_THREAD
						syscall CODE_PTR_EXIT_THREAD_SYSCALL
						jr r31
						nop
					.code CODE_PTR_FINALIZE_CALLBACK
						syscall CODE_PTR_FINALIZE_CALLBACK_SYSCALL
						jr r31
						nop
				"
                .Replace("CODE_PTR_EXIT_THREAD_SYSCALL", String.Format("0x{0:X}", HleEmulatorSpecialAddresses.CODE_PTR_EXIT_THREAD_SYSCALL))
                .Replace("CODE_PTR_FINALIZE_CALLBACK_SYSCALL", String.Format("0x{0:X}", HleEmulatorSpecialAddresses.CODE_PTR_FINALIZE_CALLBACK_SYSCALL))

                .Replace("CODE_PTR_EXIT_THREAD", String.Format("0x{0:X}", HleEmulatorSpecialAddresses.CODE_PTR_EXIT_THREAD))
                .Replace("CODE_PTR_FINALIZE_CALLBACK", String.Format("0x{0:X}", HleEmulatorSpecialAddresses.CODE_PTR_FINALIZE_CALLBACK))
            );

            RegisterModuleSyscall<ThreadManForUser>(0x206D, "sceKernelCreateThread");
            RegisterModuleSyscall<ThreadManForUser>(0x206F, "sceKernelStartThread");
            RegisterModuleSyscall<ThreadManForUser>(0x2071, "sceKernelExitDeleteThread");

            RegisterModuleSyscall<UtilsForUser>(0x20BF, "sceKernelUtilsMt19937Init");
            RegisterModuleSyscall<UtilsForUser>(0x20C0, "sceKernelUtilsMt19937UInt");

            RegisterModuleSyscall<sceDisplay>(0x213A, "sceDisplaySetMode");
            RegisterModuleSyscall<sceDisplay>(0x2147, "sceDisplayWaitVblankStart");
            RegisterModuleSyscall<sceDisplay>(0x213F, "sceDisplaySetFrameBuf");

            RegisterModuleSyscall<LoadExecForUser>(0x20EB, "sceKernelExitGame");

            RegisterModuleSyscall<sceCtrl>(0x2150, "sceCtrlPeekBufferPositive");

            RegisterModuleSyscall<Emulator>(0x1010, "emitInt");
            RegisterModuleSyscall<Emulator>(0x1011, "emitFloat");
            RegisterModuleSyscall<Emulator>(0x1012, "emitString");
            RegisterModuleSyscall<Emulator>(0x1013, "emitMemoryBlock");
            RegisterModuleSyscall<Emulator>(0x1014, "emitHex");
            RegisterModuleSyscall<Emulator>(0x1015, "emitUInt");
            RegisterModuleSyscall<Emulator>(0x1016, "emitLong");
            RegisterModuleSyscall<Emulator>(0x1017, "testArguments");

            RegisterModuleSyscall<ThreadManForUser>(HleEmulatorSpecialAddresses.CODE_PTR_EXIT_THREAD_SYSCALL,
                (Func<CpuThreadState, int>)new ThreadManForUser()._hle_sceKernelExitDeleteThread);

            RegisterModuleSyscall<Emulator>(HleEmulatorSpecialAddresses.CODE_PTR_FINALIZE_CALLBACK_SYSCALL,
                (Action<CpuThreadState>)new Emulator().finalizeCallback);
        }

        void RegisterModuleSyscall<TType>(int SyscallCode, Delegate Delegate) where TType : HleModuleHost
        {
            RegisterModuleSyscall<TType>(SyscallCode, Delegate.Method.Name);
        }

        void RegisterModuleSyscall<TType>(int SyscallCode, string FunctionName) where TType : HleModuleHost
        {
            var Delegate = ModuleManager.GetModuleDelegate<TType>(FunctionName);
            CpuProcessor.RegisterNativeSyscall(SyscallCode, (CpuThreadState, Code) =>
            {
                Delegate(CpuThreadState);
            });
        }

        public void _LoadFile(String FileName)
        {
            //GC.Collect();
            SetVirtualFolder(Path.GetDirectoryName(FileName));

            var MemoryStream = new PspMemoryStream(PspMemory);

            var Arguments = new[] {
                "ms0:/PSP/GAME/virtual/EBOOT.PBP",
            };

            Stream LoadStream = File.OpenRead(FileName);
            //using ()
            {
                List<Stream> ElfLoadStreamTry = new List<Stream>();

                var Format = new FormatDetector().DetectSubType(LoadStream);
                String Title = null;
                switch (Format)
                {
                    case FormatDetector.SubType.Pbp:
                        {
                            var Pbp = new Pbp().Load(LoadStream);
                            ElfLoadStreamTry.Add(Pbp[Pbp.Types.PspData]);
                            Logger.TryCatch(() =>
                            {
                                var ParamSfo = new Psf().Load(Pbp[Pbp.Types.ParamSfo]);

                                if (ParamSfo.EntryDictionary.ContainsKey("TITLE"))
                                {
                                    Title = (String)ParamSfo.EntryDictionary["TITLE"];
                                    DisplayConfig.Title = Title;
                                }

                                if (ParamSfo.EntryDictionary.ContainsKey("PSP_SYSTEM_VER"))
                                {
                                    HleConfig.FirmwareVersion = ParamSfo.EntryDictionary["PSP_SYSTEM_VER"].ToString();
                                }
                            });
                        }
                        break;
                    case FormatDetector.SubType.Elf:
                        ElfLoadStreamTry.Add(LoadStream);
                        break;
                    case FormatDetector.SubType.Dax:
                    case FormatDetector.SubType.Cso:
                    case FormatDetector.SubType.Iso:
                        {
                            Arguments[0] = "disc0:/PSP/GAME/SYSDIR/EBOOT.BIN";

                            var Iso = SetIso(FileName);
                            Logger.TryCatch(() =>
                            {
                                var ParamSfo = new Psf().Load(Iso.Root.Locate("/PSP_GAME/PARAM.SFO").Open());
                                Title = (String)ParamSfo.EntryDictionary["TITLE"];
                                DisplayConfig.Title = Title;
                                DisplayConfig.ID = (string)ParamSfo.EntryDictionary.GetOrDefault("DISC_ID", "");
                            });

                            var FilesToTry = new[] {
                                "/PSP_GAME/SYSDIR/BOOT.BIN",
                                "/PSP_GAME/SYSDIR/EBOOT.BIN",
                                "/PSP_GAME/SYSDIR/EBOOT.OLD",
                            };

                            foreach (var FileToTry in FilesToTry)
                            {
                                try
                                {
                                    ElfLoadStreamTry.Add(Iso.Root.Locate(FileToTry).Open());
                                }
                                catch
                                {
                                }
                                //if (ElfLoadStream.Length != 0) break;
                            }
                            /*
							if (ElfLoadStream.Length == 0)
							{
								throw (new Exception(String.Format("{0} files are empty", String.Join(", ", FilesToTry))));
							}
							*/
                        }
                        break;
                    default:
                        throw (new NotImplementedException("Can't load format '" + Format + "'"));
                }

                Exception LoadException = null;
                HleModuleGuest HleModuleGuest = null;

                foreach (var ElfLoadStream in ElfLoadStreamTry)
                {
                    try
                    {
                        LoadException = null;

                        if (ElfLoadStream.Length < 256) throw (new InvalidProgramException("File too short"));

                        HleModuleGuest = Loader.LoadModule(
                            ElfLoadStream,
                            MemoryStream,
                            MemoryManager.GetPartition(MemoryPartitions.User),
                            ModuleManager,
                            Title,
                            ModuleName: FileName,
                            IsMainModule: true
                        );

                        LoadException = null;

                        break;
                    }
                    catch (InvalidProgramException Exception)
                    {
                        LoadException = Exception;
                    }
                }

                if (LoadException != null) throw (LoadException);

                RegisterSyscalls();

                uint StartArgumentAddress = 0x08000100;
                uint EndArgumentAddress = StartArgumentAddress;

                var ArgumentsChunk = Arguments
                    .Select(Argument => Encoding.UTF8.GetBytes(Argument + "\0"))
                    .Aggregate(new byte[] { }, (Accumulate, Chunk) => (byte[])Accumulate.Concat(Chunk))
                ;

                var ReservedSyscallsPartition = MemoryManager.GetPartition(MemoryPartitions.Kernel0).Allocate(
                    0x100,
                    Name: "ReservedSyscallsPartition"
                );
                var ArgumentsPartition = MemoryManager.GetPartition(MemoryPartitions.Kernel0).Allocate(
                    ArgumentsChunk.Length,
                    Name: "ArgumentsPartition"
                );
                PspMemory.WriteBytes(ArgumentsPartition.Low, ArgumentsChunk);

                Debug.Assert(ThreadManForUser != null);

                //var MainThread = ThreadManager.Create();
                //var CpuThreadState = MainThread.CpuThreadState;
                var CurrentCpuThreadState = new CpuThreadState(CpuProcessor);
                {
                    //CpuThreadState.PC = Loader.InitInfo.PC;
                    CurrentCpuThreadState.GP = HleModuleGuest.InitInfo.GP;
                    CurrentCpuThreadState.CallerModule = HleModuleGuest;

                    int ThreadId = (int)ThreadManForUser.sceKernelCreateThread(CurrentCpuThreadState,
                        "<EntryPoint>", HleModuleGuest.InitInfo.PC, 10, 0x1000, PspThreadAttributes.ClearStack, null);

                    //var Thread = HleThreadManager.GetThreadById(ThreadId);
                    ThreadManForUser._sceKernelStartThread(CurrentCpuThreadState, ThreadId, ArgumentsPartition.Size, ArgumentsPartition.Low);
                }

                //CurrentCpuThreadState.DumpRegisters();

                //MemoryManager.GetPartition(MemoryPartitions.User).Dump();

                //ModuleManager.LoadedGuestModules.Add(HleModuleGuest);

                //MainThread.CurrentStatus = HleThread.Status.Ready;
            }
        }

        private void Main_Ended()
        {
            StoppedEndedEvent.Set();
            while (true)
            {
                ThreadTaskQueue.HandleEnqueued();
                if (!Running) return;
                Thread.Sleep(1);
            }
        }

        protected override void Main()
        {
            while (Running)
            {
#if !DO_NOT_PROPAGATE_EXCEPTIONS
                try
#endif
                {
                    // HACK! TODO: Update PspRtc every 2 thread switchings.
                    // Note: It should update the RTC after selecting the next thread to run.
                    // But currently is is not possible since updating the RTC and waking up
                    // threads has secondary effects that I have to consideer first.
                    bool TickAlternate = false;
                    //PspRtc.Update();
                    while (true)
                    {
                        ThreadTaskQueue.HandleEnqueued();

                        if (!Running) return;

                        if (!TickAlternate) PspRtc.Update();
                        TickAlternate = !TickAlternate;

                        HleThreadManager.StepNext(DoBeforeSelectingNext: () =>
                        {
                            //PspRtc.Update();
                        });
                    }
                }
#if !DO_NOT_PROPAGATE_EXCEPTIONS
                catch (Exception Exception)
                {
                    if (Exception is SceKernelSelfStopUnloadModuleException || Exception.InnerException is SceKernelSelfStopUnloadModuleException)
                    {
                        Console.WriteLine("SceKernelSelfStopUnloadModuleException");
                        Main_Ended();
                        return;
                    }

                    var ErrorOut = Console.Error;

                    ConsoleUtils.SaveRestoreConsoleState(() =>
                    {
                        Console.ForegroundColor = ConsoleColor.Red;

                        try
                        {
                            ErrorOut.WriteLine("Error on thread {0}", HleThreadManager.Current);
                            try
                            {
                                ErrorOut.WriteLine(Exception);
                            }
                            catch
                            {
                            }

                            HleThreadManager.Current.CpuThreadState.DumpRegisters(ErrorOut);

                            ErrorOut.WriteLine(
                                "Last registered PC = 0x{0:X}, RA = 0x{1:X}, RelocatedBaseAddress=0x{2:X}, UnrelocatedPC=0x{3:X}",
                                HleThreadManager.Current.CpuThreadState.PC,
                                HleThreadManager.Current.CpuThreadState.RA,
                                ElfConfig.RelocatedBaseAddress,
                                HleThreadManager.Current.CpuThreadState.PC - ElfConfig.RelocatedBaseAddress
                            );

                            ErrorOut.WriteLine("Last called syscalls: ");
                            foreach (var CalledCallback in ModuleManager.LastCalledCallbacks.Reverse())
                            {
                                ErrorOut.WriteLine("  {0}", CalledCallback);
                            }

                            foreach (var Thread in HleThreadManager.Threads)
                            {
                                ErrorOut.WriteLine("{0}", Thread.ToExtendedString());
                                ErrorOut.WriteLine(
                                    "Last valid PC: 0x{0:X} :, 0x{1:X}",
                                    Thread.CpuThreadState.LastValidPC,
                                    Thread.CpuThreadState.LastValidPC - ElfConfig.RelocatedBaseAddress
                                );
                                Thread.DumpStack(ErrorOut);
                            }

                            ErrorOut.WriteLine(
                                "Executable had relocation: {0}. RelocationAddress: 0x{1:X}",
                                ElfConfig.InfoExeHasRelocation,
                                ElfConfig.RelocatedBaseAddress
                            );

                            ErrorOut.WriteLine("");
                            ErrorOut.WriteLine("Error on thread {0}", HleThreadManager.Current);
                            ErrorOut.WriteLine(Exception);
                        }
                        catch (Exception Exception2)
                        {
                            Console.WriteLine("{0}", Exception2);
                        }
                    });

                    Main_Ended();
                }
#endif
            }
        }

        public void DumpThreads()
        {
            var ErrorOut = Console.Out;
            foreach (var Thread in HleThreadManager.Threads.ToArray())
            {
                ErrorOut.WriteLine("{0}", Thread);
                Thread.DumpStack(ErrorOut);
            }
        }
    }
}