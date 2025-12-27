using System.Collections.Generic;
using System.Threading;

namespace ScePSPUtils.Threading
{
    public class ThreadMessageBus<T>
    {
        private LinkedList<T> Queue = new LinkedList<T>();
        private ManualResetEvent HasItems = new ManualResetEvent(false);

        public void AddFirst(T item)
        {
            lock (this)
            {
                Queue.AddFirst(item);
                HasItems.Set();
            }
        }

        public void AddLast(T item)
        {
            lock (this)
            {
                Queue.AddLast(item);
                HasItems.Set();
            }
        }

        public T ReadOne()
        {
            lock (this)
            {
                HasItems.WaitOne();
                var item = Queue.First.Value;
                Queue.RemoveFirst();
                if (Queue.Count == 0) HasItems.Reset();
                return item;
            }
        }
    }
}