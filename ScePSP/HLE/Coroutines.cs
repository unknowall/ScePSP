using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ScePSP.HLE
{
    public class CoroutinePool : IDisposable
    {
        internal Coroutine CurrentCoroutine;
        internal List<Coroutine> Coroutines = new List<Coroutine>();
        internal AutoResetEvent CallerContinueEvent = new AutoResetEvent(false);
        internal Thread CallerThread;

        public Coroutine CreateCoroutine(string name, Action action)
        {
            var c = new Coroutine(name, this, action);
            Coroutines.Add(c);
            return c;
        }

        public void YieldInPool()
        {
            CurrentCoroutine.YieldInPool();
        }

        public void Dispose()
        {
            foreach (var coroutine in Coroutines.ToArray()) coroutine.Dispose();
        }
    }

    public sealed class Coroutine : IDisposable
    {
        internal CoroutinePool Pool;
        internal AutoResetEvent CoroutineContinueEvent = new AutoResetEvent(false);
        internal Task WorkerTask;
        private Thread _underlyingThread;

        public string Name { set; get; }

        Exception _rethrowException;

        private void CoroutineContinueEvent_WaitOne()
        {
            CoroutineContinueEvent.WaitOne();

            if (!IsAlive) throw new InterruptException();
        }

        private void PoolCallerContinueEvent_WaitOne()
        {
            Pool.CallerContinueEvent.WaitOne();
        }

        private sealed class InterruptException : Exception
        {
        }

        bool _mustStart;
        private string _nameField;

        internal Coroutine(string name, CoroutinePool pool, Action action)
        {
            Pool = pool;
            IsAlive = true;
            Name = name;
            _mustStart = true;
            _nameField = "Coroutine-" + name;

            //延迟到 ExecuteStep 时启动
            WorkerTask = new Task(() =>
            {
                _underlyingThread = Thread.CurrentThread;
                try
                {
                    if (string.IsNullOrEmpty(_underlyingThread.Name))
                    {
                        _underlyingThread.Name = _nameField;
                    }
                    _nameField = _underlyingThread.Name;
                }
                catch { }

                try
                {
                    CoroutineContinueEvent_WaitOne();
                    action();
                }
                catch (InterruptException)
                {
                }
                catch (Exception e)
                {
                    _rethrowException = e;
                }
                finally
                {
                    IsAlive = false;
                    pool.CallerContinueEvent.Set();
                }
            }, TaskCreationOptions.LongRunning);
        }

        public void ExecuteStep()
        {
            if (_mustStart)
            {
                _mustStart = false;
                try
                {
                    WorkerTask.Start(TaskScheduler.Default);
                }
                catch// (Exception e)
                {
                    IsAlive = false;
                    throw;
                }
            }
            //Debug.WriteLine("ExecuteStep");
            if (IsAlive)
            {
                Pool.CurrentCoroutine = this;
                Pool.CallerThread = Thread.CurrentThread;

                Pool.CallerContinueEvent.Reset();
                CoroutineContinueEvent.Set();
                PoolCallerContinueEvent_WaitOne();
            }

            if (_rethrowException != null)
            {
                try
                {
                    throw new HLETaskException("HLETask Exception", _rethrowException);
                }
                finally
                {
                    _rethrowException = null;
                }
            }
        }

        public void YieldInPool()
        {
            //Debug.WriteLine("YieldInPool");

            if (Pool.CurrentCoroutine == null)
            {
                throw new InvalidOperationException("Can't call YieldInPool outside the ExecuteStep");
            }

            if (Pool.CurrentCoroutine != this)
            {
                Debug.WriteLine("Pool.CurrentCoroutine != this");
            }

            CoroutineContinueEvent.Reset();
            Pool.CallerContinueEvent.Set();
            CoroutineContinueEvent_WaitOne();
        }

        public bool IsAlive { get; private set; }

        public bool IsCurrentlyActive => Pool.CurrentCoroutine == this;

        public void Dispose()
        {
            IsAlive = false;
            CoroutineContinueEvent.Set();
            try
            {
                if (WorkerTask != null && WorkerTask.Status == TaskStatus.Running)
                {
                    WorkerTask.Wait(50);
                }
            }
            catch { }
            Pool.Coroutines.Remove(this);
        }
    }
}