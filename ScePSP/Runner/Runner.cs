using ScePSP.Components.Display;
using ScePSP.Hle.Managers;
using ScePSP.Runner.Audio;
using ScePSP.Runner.Cpu;
using ScePSP.Runner.Display;
using ScePSP.Runner.GE;
using System.Collections.Generic;
using System.Threading;

namespace ScePSP.Runner
{
    public unsafe class Runner : IRunner, IContextInitialize
    {
        [Context]
        public CpuThread CpuThread { get; protected set; }

        [Context]
        public GEThread GEThread { get; protected set; }

        [Context]
        public AudioThread AudioThread { get; protected set; }

        [Context]
        public DisplayThread DisplayThread { get; protected set; }

        [Context]
        public DisplayConfig Config { get; protected set; }

        [Context]
        public HleThreadManager ThreadManager { get; protected set; }

        protected List<IRunner> List = new List<IRunner>();

        public bool Paused { get; protected set; }

        public bool Runing { get; protected set; }

        private Runner()
        {
        }

        void IContextInitialize.Initialize()
        {
            List.Add(CpuThread);
            List.Add(GEThread);
            List.Add(AudioThread);
            List.Add(DisplayThread);
        }

        public void StartSynchronized()
        {
            if (Runing) return;

            List.ForEach((RunnableComponent) =>
                RunnableComponent.StartSynchronized()
            );

            DisplayThread.InitRender();

            Runing = true;
            Config.Runing = true;
        }

        public void StopSynchronized()
        {
            if (!Runing) return;

            Runing = false;
            Config.Runing = false;


            AudioThread.StopSynchronized();
            DisplayThread.StopSynchronized();
            CpuThread.StopSynchronized();
            GEThread.StopSynchronized();

            CpuThread.CpuProcessor.MethodCache.Runing = false;

            foreach (var HleThread in ThreadManager.Threads)
            {
                while (HleThread.WorkThread.Running) { Thread.Sleep(1); }
            }

            CpuThread.CpuProcessor.MethodCache.Dispose();

            Runing = false;
            Config.Runing = false;
        }

        public void PauseSynchronized()
        {
            if (!Runing && Paused) return;

            List.ForEach((RunnableComponent) =>
            {
                //Console.WriteLine("Pausing {0}...", RunnableComponent);
                RunnableComponent.PauseSynchronized();
            });
            Paused = true;
        }

        public void ResumeSynchronized()
        {
            if (!Runing && !Paused) return;

            List.ForEach((RunnableComponent) =>
            {
                //Console.WriteLine("Resuming {0}...", RunnableComponent);
                RunnableComponent.ResumeSynchronized();
            });
            Paused = false;
        }
    }
}
