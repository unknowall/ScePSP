using System;
using System.Threading;

namespace ScePSP.Runner.Threading
{
    public class HleWorkThread : IDisposable
    {
        public class StopException : Exception
        {
        }

        public class HleWorkThreadException : Exception
        {
            public HleWorkThreadException(string name, Exception innerException) : base(name, innerException)
            {
            }
        }

        protected Action Action;

        protected Thread ParentThread;
        public Thread CurrentThread;
        protected ManualResetEvent ParentEvent;
        protected ManualResetEvent ThisEvent;
        protected static ThreadLocal<HleWorkThread> ThreadList = new ThreadLocal<HleWorkThread>();
        public static int ThreadLastId = 0;

        private Exception RethrowException;

        public bool Running { get; protected set; }
        protected bool Kill;
        public string Name;

        public HleWorkThread()
        {
        }

        void WaitOrParentThreadStopped()
        {
            while (true)
            {
                // If the parent thread have been stopped. We should not wait any longer.
                if (Kill || !ParentThread.IsAlive)
                {
                    break;
                }

                if (ThisEvent.WaitOne(20))
                {
                    // Signaled.
                    break;
                }
            }

            if (Kill || !ParentThread.IsAlive)
            {
                //Thread.CurrentThread.Abort();
                //throw (new StopException());
            }
        }

        public void InitAndStartStopped(string name, Action Action)
        {
            this.Action = Action;
            this.ParentThread = Thread.CurrentThread;

            ParentEvent = new ManualResetEvent(false);
            ThisEvent = new ManualResetEvent(false);

            var This = this;

            this.CurrentThread = new Thread(() =>
            {
                Console.Out.WriteLineColored(ConsoleColor.Cyan, $"HleWorkThread ({this.Name}) Start");
                ThreadList.Value = This;
                WaitOrParentThreadStopped();
                try
                {
                    Running = true;
                    Action();
                }
                catch (StopException) { }
                catch (OperationCanceledException) { }
                catch (Exception Exception)
                {
                    RethrowException = Exception;
                }
                finally
                {
                    Running = false;
                    try
                    {
                        ParentEvent.Set();
                    }
                    catch
                    {
                    }
                }
                Console.Out.WriteLineColored(ConsoleColor.Cyan, $"HleWorkThread ({this.Name}) End");
            });

            if (name == "")
                this.Name = "ID-" + ThreadLastId++;
            else
                this.Name = name;

            this.CurrentThread.Start();
        }

        public void SwitchTo()
        {
            ParentThread = Thread.CurrentThread;
            ParentEvent.Reset();
            ThisEvent.Set();

            //if (Kill) Thread.CurrentThread.Abort();

            ParentEvent.WaitOne();
            if (RethrowException != null)
            {
                try
                {
                    throw (new HleWorkThreadException("HleWorkThread Exception", RethrowException));
                }
                finally
                {
                    RethrowException = null;
                }
            }
        }

        public static void Yield()
        {
            if (ThreadList.IsValueCreated)
            {
                var GreenThread = ThreadList.Value;
                if (GreenThread.Running)
                {
                    try
                    {
                        GreenThread.Running = false;

                        GreenThread.ThisEvent.Reset();
                        GreenThread.ParentEvent.Set();
                        GreenThread.WaitOrParentThreadStopped();
                    }
                    finally
                    {
                        GreenThread.Running = true;
                    }
                }
                else
                {
                    throw (new InvalidOperationException("HleWorkThread has finalized"));
                }
            }
        }

        public void Stop()
        {
            Kill = true;
            ThisEvent.Set();
        }

        public void Dispose()
        {
            Stop();
        }

        public string ThreadName
        {
            get
            {
                return CurrentThread.Name;
            }
            set
            {
                CurrentThread.Name = Name;
            }
        }
    }
}