using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Dual.Common.Core
{
    /// <summary>
    /// http://stackoverflow.com/questions/5852863/fixed-size-queue-which-automatically-dequeues-old-values-upon-new-enques
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private readonly object syncObject = new object();

        public int Size { get; private set; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (syncObject)
            {
                while (base.Count > Size)
                {
                    T outObj;
                    base.TryDequeue(out outObj);
                }
            }
        }
    }


    // http://stackoverflow.com/questions/590069/how-would-you-code-an-efficient-circular-buffer-in-java-or-c-sharp
}
