using System;
using System.Collections.Generic;
using System.Threading;

namespace ScePSPUtils.Threading
{
    public sealed class TaskQueue
    {
        public readonly AutoResetEvent EnqueuedEvent;

        private readonly Queue<Action> _tasks = new Queue<Action>();

        public TaskQueue()
        {
            EnqueuedEvent = new AutoResetEvent(false);
        }

        public void WaitAndHandleEnqueued()
        {
            //Console.WriteLine("WaitEnqueued");
            WaitEnqueued();
            //Console.WriteLine("HandleEnqueued");
            HandleEnqueued();
        }

        public void WaitEnqueued()
        {
            int tasksCount;
            lock (_tasks) tasksCount = _tasks.Count;

            if (tasksCount == 0)
            {
                EnqueuedEvent.WaitOne();
            }
        }

        public void HandleEnqueued()
        {
            while (true)
            {
                Action action;

                lock (_tasks)
                {
                    if (_tasks.Count == 0) break;
                    action = _tasks.Dequeue();
                }

                action();
            }
        }

        public void EnqueueWithoutWaiting(Action action)
        {
            lock (_tasks)
            {
                _tasks.Enqueue(action);
                EnqueuedEvent.Set();
            }
        }

        public void EnqueueAndWaitStarted(Action action)
        {
            var Event = new AutoResetEvent(false);

            EnqueueWithoutWaiting(() =>
            {
                Event.Set();
                action();
            });

            Event.WaitOne();
        }

        public void EnqueueAndWaitStarted(Action action, TimeSpan timeout, Action actionTimeout = null)
        {
            var Event = new AutoResetEvent(false);

            EnqueueWithoutWaiting(() =>
            {
                Event.Set();
                action();
            });

            if (!Event.WaitOne(timeout))
            {
                Console.WriteLine("Timeout!");
                actionTimeout?.Invoke();
            }
        }

        public void EnqueueAndWaitCompleted(Action action)
        {
            var Event = new AutoResetEvent(false);

            EnqueueWithoutWaiting(() =>
            {
                action();
                Event.Set();
            });

            Event.WaitOne();
        }
    }
}