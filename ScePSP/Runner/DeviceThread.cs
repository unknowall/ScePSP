using ScePSP.Runner.Threading;
using ScePSPUtils;
using System;
using System.Globalization;
using System.Threading;

namespace ScePSP.Runner
{
    public abstract class DeviceThread : IRunner
    {
        static Logger Logger = Logger.GetLogger("DeviceThread");

        protected AutoResetEvent RunningUpdatedEvent = new AutoResetEvent(false);
        public bool Running = true;

        protected Thread Thread;
        protected AutoResetEvent StopCompleteEvent = new AutoResetEvent(false);
        protected AutoResetEvent PauseEvent = new AutoResetEvent(false);
        protected AutoResetEvent ResumeEvent = new AutoResetEvent(false);

        public readonly TaskQueue ThreadTaskQueue = new TaskQueue();
        protected abstract String ThreadName { get; }

        protected DeviceThread()
        {
        }

        public void StartSynchronized()
        {
            //Console.WriteLine("{0} Start!", this);

            Thread = new Thread(() =>
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
                try
                {
                    Main();
                }
                finally
                {
                    Running = false;
                    RunningUpdatedEvent.Set();
                    StopCompleteEvent.Set();
                    //Console.WriteLine("{0} Stopped!", this);
                }
            })
            {
                Name = this.ThreadName,
                IsBackground = true,
            };
            Thread.Start();
            ThreadTaskQueue.EnqueueAndWaitCompleted(() =>
            {
            });

            Console.Out.WriteLineColored(ConsoleColor.White, "## {0} Started!", this);
        }

        public void StopSynchronized()
        {
            //Logger.Notice("{0} Stop...", this);

            if (Running)
            {
                StopCompleteEvent.Reset();
                {
                    Running = false;
                    RunningUpdatedEvent.Set();
                }
                if (!StopCompleteEvent.WaitOne(1000))
                {
                    Logger.Error("Error stopping {0}", this);
                    //Thread.Abort();
                }
            }

            Logger.Notice("## {0} Stopped!", this);
        }

        public void PauseSynchronized()
        {
            Logger.Notice("{0} Pause!", this);

            ThreadTaskQueue.EnqueueAndWaitStarted(() =>
            {
                while (!PauseEvent.WaitOne(TimeSpan.FromMilliseconds(10)))
                {
                    if (!Running) break;
                    //if (MaxCounts-- < 0)
                    //{
                    //	Console.Error.WriteLine("Infinite loop detected!");
                    //	break;
                    //}
                }
            }, TimeSpan.FromSeconds(2), () =>
            {
                Console.WriteLine("Pause Timed Out!");
            });
        }

        public void ResumeSynchronized()
        {
            Logger.Notice("{0} Resume!", this);

            PauseEvent.Set();
        }

        protected abstract void Main();
    }
}
