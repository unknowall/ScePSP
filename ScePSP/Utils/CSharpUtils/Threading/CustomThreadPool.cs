using System;
using System.Collections.Generic;
using System.Threading;

namespace ScePSPUtils.Threading
{
    public class CustomThreadPool
    {
        public class WorkerThread
        {
            private bool _running;
            private readonly AutoResetEvent _moreTasksEvent;
            private Queue<Action> Tasks;
            internal long LoopIterCount;

            public WorkerThread()
            {
                _running = true;
                LoopIterCount = 0;
                _moreTasksEvent = new AutoResetEvent(false);
                Tasks = new Queue<Action>();
                var thread = new Thread(ThreadBody);
                thread.IsBackground = true;
                thread.Start();
            }

            internal void AddTask(Action task)
            {
                Tasks.Enqueue(task);
                _moreTasksEvent.Set();
            }

            internal void Stop()
            {
                AddTask(() => { _running = false; });
            }

            protected void ThreadBody()
            {
                //Console.WriteLine("CustomThreadPool.ThreadBody.Start()");
                try
                {
                    LoopIterCount = 0;
                    while (_running)
                    {
                        _moreTasksEvent.WaitOne();
                        LoopIterCount++;
                        while (Tasks.Count > 0)
                        {
                            Tasks.Dequeue()();
                        }
                    }
                }
                finally
                {
                    //Console.WriteLine("CustomThreadPool.ThreadBody.End()");
                }
            }
        }

        internal WorkerThread[] WorkerThreads;

        public CustomThreadPool(int numberOfThreads)
        {
            WorkerThreads = new WorkerThread[numberOfThreads];
            for (int n = 0; n < numberOfThreads; n++)
            {
                WorkerThreads[n] = new WorkerThread();
            }
        }

        public long GetLoopIterCount(int threadAffinity)
        {
            return WorkerThreads[threadAffinity % WorkerThreads.Length].LoopIterCount;
        }

        public void AddTask(int threadAffinity, Action task)
        {
            WorkerThreads[threadAffinity % WorkerThreads.Length].AddTask(task);
        }

        public void Stop()
        {
            foreach (var workerThread in WorkerThreads)
            {
                workerThread.Stop();
            }
        }
    }
}