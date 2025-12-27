using ScePSP.Runner.Tasks;
using ScePSP.Runner.Tasks.Audio;
using ScePSP.Runner.Tasks.Cpu;
using ScePSP.Runner.Tasks.Display;
using ScePSP.Runner.Tasks.Gpu;
using System;
using System.Collections.Generic;

namespace ScePSP.Runner
{
    public class PspRunner : IRunnableTask, IInjectInitialize
    {
        [Inject]
        public CpuTask CpuTask { get; protected set; }

        [Inject]
        public GpuTask GpuTask { get; protected set; }

        [Inject]
        public AudioTask AudioTask { get; protected set; }

        [Inject]
        public DisplayTask DisplayTask { get; protected set; }

        protected List<IRunnableTask> RunnableTaskList = new List<IRunnableTask>();

        public bool Paused { get; protected set; }

        private PspRunner()
        {
        }

        void IInjectInitialize.Initialize()
        {
            RunnableTaskList.Add(CpuTask);
            RunnableTaskList.Add(GpuTask);
            RunnableTaskList.Add(AudioTask);
            RunnableTaskList.Add(DisplayTask);
        }

        public void StartSynchronized()
        {
            RunnableTaskList.ForEach(runnableComponent =>
                runnableComponent.StartSynchronized()
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