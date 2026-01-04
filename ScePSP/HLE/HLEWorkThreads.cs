using System;
using System.Threading;

namespace ScePSP.HLE
{
    public class HLETaskException : Exception
    {
        public HLETaskException(string name, Exception innerException) : base(name, innerException)
        {
        }
    }

    public class HLEWorkThreads : IDisposable
    {
        public bool Running { get; protected set; }

        public string Name
        {
            get
            {
                return this.CurrentThread.Name;
            }
            set
            {
                this.CurrentThread.Name = value;
            }
        }

        protected Action Action;

        protected Thread ParentThread;

        protected Thread CurrentThread;

        protected Semaphore ParentSemaphore;

        protected Semaphore ThisSemaphore;

        protected static ThreadLocal<HLEWorkThreads> ThisThreadList = new ThreadLocal<HLEWorkThreads>();

        public static int ThreadLastId = 0;

        public static Thread MonitorThread;

        private Exception RethrowException;

        protected bool Kill;

        public class StopException : Exception
        {
        }

        private bool ThisSemaphoreWaitOrParentThreadStopped()
        {
            while (!this.Kill && this.ParentThread.IsAlive && !this.ThisSemaphore.WaitOne(20))
            {
            }
            if (this.Kill || !this.ParentThread.IsAlive)
            {
                return true;
            }
            return false;
        }

        public void InitAndStartStopped(Action Action)
        {
            this.Action = Action;
            this.ParentThread = Thread.CurrentThread;
            this.ParentSemaphore = new Semaphore(1, 1);
            this.ParentSemaphore.WaitOne();
            this.ThisSemaphore = new Semaphore(1, 1);
            this.ThisSemaphore.WaitOne();
            HLEWorkThreads This = this;
            this.CurrentThread = new Thread(delegate ()
            {
                Console.WriteLine($"PSPThread {this.CurrentThread.Name} Start");
                HLEWorkThreads.ThisThreadList.Value = This;
                if (this.ThisSemaphoreWaitOrParentThreadStopped())
                {
                    this.Running = false;

                    this.ParentSemaphore.Release();

                    Console.WriteLine($"PSPThread {this.CurrentThread.Name} End");

                    return;
                }
                try
                {
                    Running = true;
                    Action();
                }
                catch (HLEWorkThreads.StopException)
                {
                }
                catch (Exception rethrowException)
                {
                    RethrowException = rethrowException;
                }
                finally
                {
                    Running = false;

                    try
                    {
                        ParentSemaphore.Release();
                    }
                    catch { }

                    Console.WriteLine($"PSPThread {this.CurrentThread.Name} End");
                }
            });
            this.CurrentThread.Name = "PSPThread - " + HLEWorkThreads.ThreadLastId++;
            this.CurrentThread.Start();
        }

        public void SwitchTo()
        {
            this.ParentThread = Thread.CurrentThread;
            this.ThisSemaphore.Release();
            if (this.Kill)
            {
                //TODO: 需要终止 psp 执行 Action 的线程
                //Thread.CurrentThread.Abort();
                Console.Out.WriteLineColored(ConsoleColor.Red, $" !!! can't Kill PSPThread {this.CurrentThread.Name}");
            }
            this.ParentSemaphore.WaitOne();
            if (this.RethrowException != null)
            {
                try
                {
                    throw new HLETaskException("PSPThread Exception", this.RethrowException);
                }
                finally
                {
                    this.RethrowException = null;
                }
            }
        }

        public static void Yield()
        {
            if (HLEWorkThreads.ThisThreadList.IsValueCreated)
            {
                HLEWorkThreads value = HLEWorkThreads.ThisThreadList.Value;
                if (value.Running)
                {
                    try
                    {
                        value.Running = false;
                        value.ParentSemaphore.Release();
                        if (value.ThisSemaphoreWaitOrParentThreadStopped())
                        {
                            //TODO: 需要终止 psp 执行 Action 的线程
                            //value.CurrentThread.Abort();
                            Console.Out.WriteLineColored(ConsoleColor.Red, $" !!! Yield can't Kill PSPThread {value.CurrentThread.Name}");
                        }
                        return;
                    }
                    finally
                    {
                        value.Running = true;
                    }
                }
                throw new InvalidOperationException("PSPThread has finalized");
            }
        }

        public void Stop()
        {
            this.Kill = true;
            this.ThisSemaphore.Release();
        }

        public void Dispose()
        {
            this.Stop();
        }
    }
}