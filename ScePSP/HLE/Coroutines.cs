using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

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
        internal Thread WorkerThread;

        public bool IsAlive { get; private set; }

        public bool IsCurrentlyActive => Pool.CurrentCoroutine == this;

        public string Name { set; get; }

        private bool MustStart;

        Exception RethrowException;

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

        internal Coroutine(string name, CoroutinePool pool, Action action)
        {
            Pool = pool;
            IsAlive = true;
            WorkerThread = new Thread(delegate ()
            {
                Console.WriteLine($"Coroutine Start ({this.Name})");
                try
                {

                    CoroutineContinueEvent_WaitOne();
                    action();
                }
                catch (Coroutine.InterruptException)
                {
                }
                catch (Exception rethrowException)
                {

                    RethrowException = rethrowException;
                }
                finally
                {
                    Console.WriteLine($"Coroutine Finished ({this.Name})");

                    IsAlive = false;
                    Pool.CallerContinueEvent.Set();
                }
            })
            {
                CurrentCulture = new CultureInfo("en-US"),
                IsBackground = true
            };
            this.Name = Name;
            this.MustStart = true;
        }

        public void ExecuteStep()
        {
            if (MustStart)
            {
                MustStart = false;
                WorkerThread.Name = "Coroutine-" + this.Name;
                WorkerThread.Start();
            }
            if (IsAlive)
            {
                Pool.CurrentCoroutine = this;
                Pool.CallerThread = Thread.CurrentThread;
                Pool.CallerContinueEvent.Reset();
                CoroutineContinueEvent.Set();
                PoolCallerContinueEvent_WaitOne();
            }
            if (RethrowException != null)
            {
                try
                {
                    throw new HLETaskException("Coroutine Exception", this.RethrowException);
                }
                finally
                {
                    RethrowException = null;
                }
            }
        }

        public void YieldInPool()
        {
            if (Pool.CurrentCoroutine == null)
            {
                throw new InvalidOperationException("Can't call YieldInPool outside the ExecuteStep");
            }

            if (Pool.CurrentCoroutine != this)
            {
                Console.WriteLine("Pool.CurrentCoroutine != this");
            }

            CoroutineContinueEvent.Reset();
            Pool.CallerContinueEvent.Set();
            CoroutineContinueEvent_WaitOne();
        }

        public void Dispose()
        {
            IsAlive = false;
            CoroutineContinueEvent.Set();
            Pool.Coroutines.Remove(this);
        }
    }
}