using ScePSP.cheats;
using ScePSP.Core;
using ScePSP.Core.AudioBackEnd;
using ScePSP.Core.Components.Display;
using ScePSP.Core.Components.Rtc;
using ScePSP.Core.Cpu;
using ScePSP.Core.GpuBackEnd;
using ScePSP.Core.Memory;
using ScePSP.Hle;
using ScePSP.Hle.Loader;
using ScePSP.Hle.Managers;
using ScePSP.Hle.Modules.threadman;
using ScePSP.Hle.Vfs;
using ScePSP.Hle.Vfs.Emulator;
using ScePSP.Runner;
using ScePSP.TextureHook;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace ScePSP
{
    public class PspEmulator : IDisposable
    {
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
        public bool Paused => PSPDrivers.PspRunner?.Paused ?? false;

        public void Pause()
        {
            if (!Paused)
            {
                Console.WriteLine("Pausing...");
                PSPDrivers.PspRunner.PauseSynchronized();
                Console.WriteLine("Pausing...Ok");
            }
        }

        public void Resume()
        {
            if (Paused)
            {
                PSPDrivers.PspRunner.ResumeSynchronized();
            }
        }

        public PspEmulator()
        {
            PSPDrivers.Config.StoredConfig = PspStoredConfig.Load();
        }

        public void Start(string File, bool TraceSyscalls = false, bool TrackCallStack = true, IntPtr GpuWindowHandle = default)
        {
            PSPDrivers.initialize(PSPDrivers.PspGpuType.OpenGL, PSPDrivers.PspAudioType.SDL);

            PSPDrivers.Config.DisplayConfig.WindowHandle = GpuWindowHandle;
            PSPDrivers.Config.CpuConfig.DebugSyscalls = TraceSyscalls;
            PSPDrivers.Config.CpuConfig.TrackCallStack = TrackCallStack;

            PSPDrivers.PspRunner.StartSynchronized();

            LoadFile(File);
        }

        public void Stop()
        {
            PSPDrivers.PspRunner?.StopSynchronized();

            PSPDrivers.Config.StoredConfig?.Save();

            PSPDrivers.free();
        }

        public void LoadFile(string FileName)
        {
            Console.WriteLine("LoadFile...{0}", FileName);

            if (!File.Exists(FileName))
            {
                throw new Exception($"File '{FileName}' doesn't exists");
            }

            //for SFA3 Crash
            //PSPDrivers.Tasks.CpuTask.StartSynchronized(true);

            PSPDrivers.PspRunner.CpuTask.ThreadTaskQueue.EnqueueAndWaitCompleted(() =>
            {
                PSPDrivers.PspRunner.CpuTask._LoadFile(FileName);
            });
        }

        public void ShowDebugInformation()
        {
            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine("ShowDebugInformation:");
            Console.WriteLine("-----------------------------------------------------------------");
            try
            {
                foreach (var Pair in PSPDrivers.CPU.GlobalInstructionStats.OrderBy(Pair => Pair.Value))
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
                foreach (var CalledCallback in PSPDrivers.HLE.HleModuleManager.LastCalledCallbacks
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
                PSPDrivers.PspRunner.CpuTask.DumpThreads();
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

        void IDisposable.Dispose()
        {
            Stop();

            Console.WriteLine("PspEmulator.Dispose()");
        }
    }
}