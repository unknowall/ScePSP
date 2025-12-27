using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScePSP.HLE
{
    public class HLETaskException : Exception
    {
        public HLETaskException(string name, Exception innerException) : base(name, innerException)
        {
        }
    }

    public class HLETasks : IDisposable
    {
        public class StopException : Exception
        {
        }

        protected Action Action;

        // Parent continues to be an OS thread that switches into the green thread.
        protected Thread ParentThread;

        protected Task CurrentTask;
        private Thread CurrentUnderlyingThread;

        protected ManualResetEvent ParentEvent;
        protected ManualResetEvent ThisEvent;

        protected static ThreadLocal<HLETasks> ThisGreenThreadList = new ThreadLocal<HLETasks>();

        public static int GreenThreadLastId = 0;

        private Exception RethrowException;

        public bool Running { get; protected set; }

        protected bool Kill;

        private CancellationTokenSource cts = new CancellationTokenSource();

        private string _nameField;

        public HLETasks()
        {
        }

        ~HLETasks()
        {
        }

        void ThisSemaphoreWaitOrParentThreadStopped()
        {
            while (true)
            {
                if (Kill || ParentThread == null || !ParentThread.IsAlive)
                {
                    break;
                }

                if (ThisEvent.WaitOne(20))
                {
                    // Signaled.
                    break;
                }
            }

            if (Kill || ParentThread == null || !ParentThread.IsAlive)
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
            ParentThread = Thread.CurrentThread;

            ParentEvent = new ManualResetEvent(false);
            ThisEvent = new ManualResetEvent(false);

            var This = this;

            var token = cts.Token;
            var id = GreenThreadLastId++;

            CurrentTask = Task.Factory.StartNew(() =>
            {
                CurrentUnderlyingThread = Thread.CurrentThread;
                try
                {
                    if (string.IsNullOrEmpty(CurrentUnderlyingThread.Name))
                    {
                        CurrentUnderlyingThread.Name = "HLETask-" + id;
                    }
                    _nameField = CurrentUnderlyingThread.Name;
                }
                catch { }

                while (!token.IsCancellationRequested)
                {
                    ThisGreenThreadList.Value = This;

                    ThisSemaphoreWaitOrParentThreadStopped();

                    if (token.IsCancellationRequested) break;

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
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
                    CurrentTask?.Wait();
                }
                catch { }
            }
            ParentEvent.WaitOne();
            if (RethrowException != null)
            {
                try
                {
                    throw new HLETaskException("HLETask Exception", RethrowException);
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
                    throw new InvalidOperationException("HLETask has finalized");
                }
            }
        }

        public static void StopAll()
        {
            try
            {
                var values = ThisGreenThreadList.Values;
                if (values != null)
                {
                    foreach (var gt in values)
                    {
                        try
                        {
                            gt?.Stop();
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                try
                {
                    if (ThisGreenThreadList.IsValueCreated)
                    {
                        ThisGreenThreadList.Value?.Stop();
                    }
                }
                catch
                {
                }
            }
            catch
            {
            }
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
                if (CurrentTask != null)
                {
                    try
                    {
                        CurrentTask.Wait();
                    }
                    catch (AggregateException ae)
                    {
                        ae.Handle(e => e is OperationCanceledException);
                    }
                    catch { }
                }
            }
            catch { }
        }

        public void Dispose()
        {
            Stop();
        }

        public string Name
        {
            get
            {
                try
                {
                    if (CurrentUnderlyingThread != null) return CurrentUnderlyingThread.Name;
                }
                catch { }
                return _nameField;
            }
            set
            {
                _nameField = value;
                try
                {
                    if (CurrentUnderlyingThread != null) CurrentUnderlyingThread.Name = value;
                }
                catch { }
            }
        }
    }

}