using ScePSP.Emulator;
using ScePSP.Utils;
using ScePSPUtils;
using System;
using System.Globalization;
using System.Threading;

namespace ScePSP.Runner.Tasks
{
    public abstract class PspMainTask : IRunnableTask
    {
        static Logger Logger = Logger.GetLogger("MainTask");

        public bool Running = false;

        protected abstract string ThreadName { get; }

        protected Thread Task;

        public readonly TaskQueue ThreadTaskQueue = new TaskQueue();

        protected AutoResetEvent RunningUpdatedEvent = new AutoResetEvent(false);
        protected AutoResetEvent StopCompleteEvent = new AutoResetEvent(false);
        protected AutoResetEvent PauseEvent = new AutoResetEvent(false);
        protected AutoResetEvent ResumeEvent = new AutoResetEvent(false);

        protected PspMainTask()
        {
        }

        public void StartSynchronized(bool ForceRun = false)
        {
            if (Running && !ForceRun)
            {
                return;
            }

            //Console.WriteLine("Task {0} Starting...", this);

            Task = new Thread(delegate ()
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                Running = true;
                try
                {
                    Main();
                }
                finally
                {
                    Running = false;
                    RunningUpdatedEvent.Set();
                    StopCompleteEvent.Set();
                    //Console.WriteLine("Task {0} Finished!", this);
                }
            })
            {
                Name = this.ThreadName,
                IsBackground = true
            };

            Task.Start();

            ThreadTaskQueue.EnqueueAndWaitCompleted(delegate
            {
            });
        }

        public void StopSynchronized()
        {
            //Console.WriteLine("Task {0} Stoping...", this);

            if (Running)
            {
                StopCompleteEvent.Reset();
                Running = false;
                RunningUpdatedEvent.Set();
                if (!StopCompleteEvent.WaitOne(1000))
                {
                    Logger.Error("Error stopping {0}", this);
                    //Task.Abort();
                }
            }

            Console.WriteLine("Task {0} Stopped!", this);
        }

        public void PauseSynchronized()
        {
            Console.WriteLine("Task {0} Pauseing!", this);

            ThreadTaskQueue.EnqueueAndWaitStarted(() =>
            {
                while (!PauseEvent.WaitOne(10.Milliseconds()))
                {
                    if (!Running) break;
                }
            }, TimeSpan.FromSeconds(2), () => { Console.WriteLine("Timed Out!"); });
        }

        public void ResumeSynchronized()
        {
            if (!Running) return;

            Console.WriteLine("Task {0} Resumeing!", this);

            PauseEvent.Set();
        }

        protected abstract void Main();
    }
}