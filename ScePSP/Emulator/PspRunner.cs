using ScePSP.Runner.Tasks;
using ScePSP.Runner.Tasks.Audio;
using ScePSP.Runner.Tasks.Cpu;
using ScePSP.Runner.Tasks.Display;
using ScePSP.Runner.Tasks.Gpu;
using System;
using System.Collections.Generic;

namespace ScePSP.Runner
{
    public class PspRunner : IRunnableTask
    {
        public CpuTask CpuTask => PSPDrivers.Tasks.CpuTask;

        public GpuTask GpuTask => PSPDrivers.Tasks.GpuTask;

        public AudioTask AudioTask => PSPDrivers.Tasks.AudioTask;

        public DisplayTask DisplayTask => PSPDrivers.Tasks.DisplayTask;

        protected List<IRunnableTask> RunnableTaskList = new List<IRunnableTask>();

        public bool Paused { get; protected set; }

        public PspRunner()
        {
            RunnableTaskList.Add(CpuTask);
            RunnableTaskList.Add(GpuTask);
            RunnableTaskList.Add(AudioTask);
            RunnableTaskList.Add(DisplayTask);
        }

        public void StartSynchronized(bool ForceRun = false)
        {
            RunnableTaskList.ForEach(runnableComponent =>
                runnableComponent.StartSynchronized(ForceRun)
            );
        }

        public void StopSynchronized()
        {
            Console.WriteLine("Stopping!");
            RunnableTaskList.ForEach(runnableComponent =>
                runnableComponent.StopSynchronized()
            );
            Console.WriteLine("Stopped!");
        }

        public void PauseSynchronized()
        {
            RunnableTaskList.ForEach(runnableComponent =>
            {
                Console.Write("Pausing {0}...", runnableComponent);
                runnableComponent.PauseSynchronized();
                Console.WriteLine("Ok");
            });
            Paused = true;
        }

        public void ResumeSynchronized()
        {
            RunnableTaskList.ForEach(runnableComponent =>
            {
                Console.Write("Resuming {0}...", runnableComponent);
                runnableComponent.ResumeSynchronized();
                Console.WriteLine("Ok");
            });
            Paused = false;
        }
    }
}