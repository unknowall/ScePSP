using ScePSP.Core;
using System;
using System.IO;
using System.Linq;

namespace ScePSP
{
    public class PspEmulator : IDisposable
    {
        public bool IsPaused() => Paused;

        public bool Paused => PSPDrivers.Runner?.Paused ?? false;

        public bool Runing;

        public void Pause()
        {
            if (!Paused)
            {
                Console.WriteLine("Pausing...");
                PSPDrivers.Runner.PauseSynchronized();
                Console.WriteLine("Pausing...Ok");
            }
        }

        public void Resume()
        {
            if (Paused)
            {
                PSPDrivers.Runner.ResumeSynchronized();
            }
        }

        public PspEmulator()
        {
            PSPDrivers.Config.StoredConfig = PspStoredConfig.Load();
        }

        public void Start(string File, bool TraceSyscalls = false, bool TrackCallStack = true, IntPtr WindowHandle = default)
        {
            PSPDrivers.initialize(PSPDrivers.PspGpuType.OpenGL, PSPDrivers.PspAudioType.SDL, WindowHandle);

            PSPDrivers.Config.CpuConfig.DebugSyscalls = TraceSyscalls;
            PSPDrivers.Config.CpuConfig.TrackCallStack = false;

            PSPDrivers.Config.HleConfig.DebugSyscalls = TraceSyscalls;
            PSPDrivers.Config.HleConfig.UseCoRoutines = true;

            PSPDrivers.Runner.StartSynchronized();

            LoadFile(File);
        }

        public void Stop()
        {
            PSPDrivers.Runner?.StopSynchronized();

            PSPDrivers.GeBackEnd?.StopSynchronized();

            PSPDrivers.Config.StoredConfig?.Save();

            PSPDrivers.free();

            Runing = false;
        }

        private void LoadFile(string FileName)
        {
            Console.WriteLine("LoadFile...{0}", FileName);

            if (!File.Exists(FileName))
            {
                throw new Exception($"File '{FileName}' doesn't exists");
            }

            PSPDrivers.Runner.CpuTask.ThreadTaskQueue.EnqueueAndWaitCompleted(() =>
            {
                PSPDrivers.Runner.CpuTask._LoadFile(FileName);
            });

            Runing = true;
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
                foreach (var CalledCallback in PSPDrivers.HLE.HleModuleManager.LastCalledCallbacks.ToArray().Reverse())
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
                PSPDrivers.Runner.CpuTask.DumpThreads();
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
        }
    }
}