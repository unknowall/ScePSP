using System;
using System.Threading;

namespace ScePSPUtils.Threading
{
    public class GreenThread : IDisposable
    {
        public class StopException : Exception
        {
        }

        protected Action Action;

        protected Thread ParentThread;
        protected Thread CurrentThread;

        protected ManualResetEvent ParentEvent;
        protected ManualResetEvent ThisEvent;

        protected static ThreadLocal<GreenThread> ThisGreenThreadList = new ThreadLocal<GreenThread>();

        public static int GreenThreadLastId = 0;

        public static Thread MonitorThread;

        private Exception RethrowException;

        public bool Running { get; protected set; }

        protected bool Kill;

        private CancellationTokenSource cts = new CancellationTokenSource();

        public GreenThread()
        {
        }

        ~GreenThread()
        {
        }

        void ThisSemaphoreWaitOrParentThreadStopped()
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
                try
                {
                    cts.Cancel();
                }
                catch { }
                return;
            }
        }

        public void InitAndStartStopped(Action Action)
        {
            this.Action = Action;
            this.ParentThread = Thread.CurrentThread;

            ParentEvent = new ManualResetEvent(false);
            ThisEvent = new ManualResetEvent(false);

            var This = this;

            this.CurrentThread = new Thread(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    ThisGreenThreadList.Value = This;

                    ThisSemaphoreWaitOrParentThreadStopped();

                    if (cts.Token.IsCancellationRequested) break;

                    try
                    {
                        Running = true;
                        Action();
                    }
                    catch (StopException)
                    {
                    }
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
                }
                //Console.WriteLine("GreenThread.Running: {0}", Running);
            });

            this.CurrentThread.Name = "GreenThread-" + GreenThreadLastId++;

            this.CurrentThread.Start();
        }

        public void SwitchTo()
        {
            ParentThread = Thread.CurrentThread;
            ParentEvent.Reset();
            ThisEvent.Set();
            if (Kill)
            {
                try
                {
                    cts.Cancel();
                }
                catch { }
                try
                {
                    CurrentThread.Join();
                }
                catch { }
            }
            //ThisSemaphoreWaitOrParentThreadStopped();
            ParentEvent.WaitOne();
            if (RethrowException != null)
            {
                try
                {
                    //StackTraceUtils.PreserveStackTrace(RethrowException);
                    throw (new GreenThreadException("GreenThread Exception", RethrowException));
                }
                finally
                {
                    RethrowException = null;
                }
            }
        }

        public static void Yield()
        {
            if (ThisGreenThreadList.IsValueCreated)
            {
                var GreenThread = ThisGreenThreadList.Value;
                if (GreenThread.Running)
                {
                    try
                    {
                        GreenThread.Running = false;

                        GreenThread.ThisEvent.Reset();
                        GreenThread.ParentEvent.Set();
                        GreenThread.ThisSemaphoreWaitOrParentThreadStopped();
                    }
                    finally
                    {
                        GreenThread.Running = true;
                    }
                }
                else
                {
                    throw (new InvalidOperationException("GreenThread has finalized"));
                }
            }
        }

        public static void StopAll()
        {
            throw (new NotImplementedException());
        }

        public void Stop()
        {
            Kill = true;
            ThisEvent.Set();
            try
            {
                cts.Cancel();
            }
            catch { }
            try
            {
                CurrentThread.Join();
            }
            catch { }
            //CurrentThread.Abort();
        }

        public void Dispose()
        {
            Stop();
        }

        public string Name
        {
            get
            {
                return CurrentThread.Name;
            }
            set
            {
                if (CurrentThread != null) CurrentThread.Name = value;
            }
        }
    }

}