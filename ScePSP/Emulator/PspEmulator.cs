using ScePSP.cheats;
using ScePSP.Core;
using ScePSP.Core.Components.Display;
using ScePSP.Core.Cpu;
using ScePSP.Core.Gpu;
using ScePSP.Core.Memory;
using ScePSP.Hle;
using ScePSP.Hle.Loader;
using ScePSP.Hle.Managers;
using ScePSP.Runner;
using ScePSP.TextureHook;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace ScePSP
{
    public class PspEmulator : IGuiExternalInterface, IDisposable
    {
        [Inject] public CpuConfig CpuConfig;

        [Inject] GpuConfig GpuConfig;

        [Inject] HleConfig HleConfig;

        [Inject] DisplayConfig DisplayConfig;

        [Inject] PspMemory PspMemory;

        [Inject] ElfConfig ElfConfig;

        [Inject] GpuImpl GpuImpl;

        [Inject] PspDisplay PspDisplay;

        [Inject] CWCheatPlugin CWCheatPlugin;

        [Inject] TextureHookPlugin TextureHookPlugin;

        [Inject] InjectMessageBus MessageBus;

        public InjectContext InjectContext
        {
            get
            {
                lock (this) return _InjectContext;
            }
        }

        [Inject] private InjectContext _InjectContext;

        [Inject] public PspRunner PspRunner;

        PspStoredConfig StoredConfig;

        public void PauseResume(Action Action)
        {
            if (Paused)
            {
                Action();
            }
            else
            {
                Pause();
                try
                {
                    Action();
                }
                finally
                {
                    Resume();
                }
            }
        }

        public bool IsPaused() => Paused;
        public bool Paused => PspRunner?.Paused ?? false;

        public void Pause()
        {
            if (!Paused)
            {
                Console.WriteLine("Pausing...");
                PspRunner.PauseSynchronized();
                Console.WriteLine("Pausing...Ok");
            }
        }

        public void Resume()
        {
            if (Paused)
            {
                PspRunner.ResumeSynchronized();
            }
        }

        public PspEmulator()
        {
            StoredConfig = PspStoredConfig.Load();
        }

        public void StartAndLoad(
            string File, Action<PspEmulator> GuiRunner = null,
            bool TraceSyscalls = false, bool TrackCallStack = true,
            IntPtr GpuWindowHandle = default,
            bool EnableMpeg = false
        )
        {
            Start(() =>
                    {
                        LoadFile(File);
                    }, GuiRunner, TraceSyscalls, TrackCallStack, GpuWindowHandle
                );
        }

        public void Start(Action CallbackOnInit = null, Action<PspEmulator> GuiRunner = null, bool TraceSyscalls = false, bool TrackCallStack = true, IntPtr GpuWindowHandle = default)
        {
            try
            {
                CpuConfig.DebugSyscalls = TraceSyscalls;
                CpuConfig.TrackCallStack = TrackCallStack;
                DisplayConfig.WindowHandle = GpuWindowHandle;

                // Creates a temporal context.
                //PspEmulatorContext = new PspEmulatorContext(PspConfig);

                PspRunner.StartSynchronized();

                CallbackOnInit?.Invoke();

                Thread.CurrentThread.Name = "GuiThread";

                //ContextInitialized.WaitOne();

                GuiRunner?.Invoke(this);

                PspRunner.StopSynchronized();
            }
            catch (Exception Exception)
            {
                Console.Error.WriteLine(Exception);
            }
            finally
            {
                StoredConfig.Save();
            }

            Console.WriteLine("Exiting...");
            //foreach (var thread in Process.GetCurrentProcess().Threads.Cast<ProcessThread>())
            //{
            //	Console.WriteLine("Thread: {0}, {1}", thread.ThreadState, (thread.ThreadState == System.Diagnostics.ThreadState.Wait) ?  thread.WaitReason.ToString() : "");
            //}
            //Environment.Exit(0);
            return;
        }

        public void LoadFile(string FileName)
        {
            Console.WriteLine("LoadFile...{0}", FileName);
            if (!File.Exists(FileName))
            {
                throw new Exception($"File '{FileName}' doesn't exists");
            }

            PspRunner.StartSynchronized();

            MessageBus.Dispatch(new LoadFileMessage() { FileName = FileName });

            PspRunner.CpuTask.ThreadTaskQueue.EnqueueAndWaitCompleted(() =>
            {
                PspRunner.CpuTask._LoadFile(FileName);
            });
        }

        public void ShowDebugInformation()
        {
            var CpuProcessor = InjectContext.GetInstance<CpuProcessor>();
            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine("ShowDebugInformation:");
            Console.WriteLine("-----------------------------------------------------------------");
            try
            {
                foreach (var Pair in CpuProcessor.GlobalInstructionStats.OrderBy(Pair => Pair.Value))
                {
                    Console.WriteLine("{0} -> {1}", Pair.Key, Pair.Value);
                }
            }
            catch (Exception Exception)
            {
                Console.Error.WriteLine(Exception);
            }

            /*
            Console.WriteLine("-----------------------------------------------------------------");
            foreach (var Pair in CpuProcessor.GlobalInstructionStats.OrderBy(Pair => Pair.Key)) Console.WriteLine("{0} -> {1}", Pair.Key, Pair.Value);
            */

            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine("Last called syscalls: ");
            try
            {
                foreach (var CalledCallback in InjectContext.GetInstance<HleModuleManager>().LastCalledCallbacks
                    .ToArray().Reverse())
                {
                    Console.WriteLine("  {0}", CalledCallback);
                }
            }
            catch (Exception Exception)
            {
                Console.Error.WriteLine(Exception);
            }

            Console.WriteLine("-----------------------------------------------------------------");
            try
            {
                PspRunner.CpuTask.DumpThreads();
            }
            catch (Exception Exception)
            {
                Console.Error.WriteLine(Exception);
            }

            Console.WriteLine("-----------------------------------------------------------------");

            //foreach (var Instruction in CpuProcessor.GlobalInstructionStats.OrderBy(Item => Item.Key))
            //{
            //	Console.WriteLine("{0}: {1}", Instruction.Key, Instruction.Value);
            //}
            //
            //Console.WriteLine("-----------------------------------------------------------------");
        }

        public void CaptureGpuFrame()
        {
            InjectContext.GetInstance<GpuProcessor>().CaptureFrame();
        }

        public object GetCpuProcessor()
        {
            return InjectContext.GetInstance<CpuProcessor>();
        }

        void IDisposable.Dispose()
        {
            //Console.WriteLine("PspEmulator.Dispose()");
            InjectContext.Dispose();
            _InjectContext = null;
        }
    }
}