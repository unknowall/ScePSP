using ScePSP.Emulator;
using ScePSP.Utils;
using ScePSPUtils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScePSP.Runner.Tasks
{
    public abstract class PspDeviceTask : IRunnableTask
    {
        static Logger Logger = Logger.GetLogger("DeviceTask");

        protected AutoResetEvent RunningUpdatedEvent = new AutoResetEvent(false);
        public bool Running = false;

        protected Task Task;
        protected CancellationTokenSource TaskCts;
        protected AutoResetEvent StopCompleteEvent = new AutoResetEvent(false);
        protected AutoResetEvent PauseEvent = new AutoResetEvent(false);
        protected AutoResetEvent ResumeEvent = new AutoResetEvent(false);

        public readonly TaskQueue ThreadTaskQueue = new TaskQueue();
        protected abstract string ThreadName { get; }

        protected PspDeviceTask()
        {
        }

        public void StartSynchronized()
        {
            if (Running) return;

            var ElapsedTime = Logger.Measure(() =>
            {
                TaskCts = new CancellationTokenSource();
                var token = TaskCts.Token;

                Task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Running = true;

                        Main();
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e);
                    }
                    finally
                    {
                        Running = false;
                        RunningUpdatedEvent.Set();
                        StopCompleteEvent.Set();
                        //Console.WriteLine("Task {0} Stopped!", this);
                    }
                }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                ThreadTaskQueue.EnqueueAndWaitCompleted(() => { });
            });
            //Logger.Notice("Component {0} Started! StartedTime({1})", this, ElapsedTime.TotalSeconds);
            //Console.WriteLine("Component {0} Started! StartedTime({1})", this, ElapsedTime.TotalSeconds);
        }

        public void StopSynchronized()
        {
            Logger.Notice("Task {0} StopSynchronized...", this);
            var ElapsedTime = Logger.Measure(() =>
            {
                if (Running)
                {
                    StopCompleteEvent.Reset();

                    Running = false;
                    RunningUpdatedEvent.Set();

                    try
                    {
                        TaskCts?.Cancel();

                        if (Task != null)
                        {
                            bool completed = Task.Wait(1000);
                            if (!completed)
                            {
                                Logger.Error("Error stopping {0}: task did not complete within timeout", this);
                            }
                        }
                    }
                    catch (AggregateException ae)
                    {
                        ae.Handle(ex => ex is OperationCanceledException);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error stopping {0}: {1}", this, ex);
                    }
                    finally
                    {
                        Task = null;
                        TaskCts?.Dispose();
                        TaskCts = null;
                    }
                }
            });
            Logger.Notice("Stopped! {0}", ElapsedTime);
        }

        public void PauseSynchronized()
        {
            Logger.Notice("Task {0} PauseSynchronized!", this);

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

            Logger.Notice("Task {0} ResumeSynchronized!", this);

            PauseEvent.Set();
        }

        protected abstract void Main();
    }
}