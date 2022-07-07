using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dual.Common.Core.DataTypes
{
    // https://devblogs.microsoft.com/pfxteam/asynclazyt/
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory)
            : this(valueFactory, LazyThreadSafetyMode.None)
        { }
        public AsyncLazy(Func<T> valueFactory, LazyThreadSafetyMode mode) :
            base(() => Task.Factory.StartNew(valueFactory), mode)
        { }

        public AsyncLazy(Func<Task<T>> taskFactory)
            : this(taskFactory, LazyThreadSafetyMode.None)
        { }
        public AsyncLazy(Func<Task<T>> taskFactory, LazyThreadSafetyMode mode) :
            base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap(), mode)
        { }

        public TaskAwaiter<T> GetAwaiter() { return Value.GetAwaiter(); }
    }
}


/*
static AsyncLazy<string> m_data = new AsyncLazy<string>(async delegate
{
    WebClient client = new WebClient();
    return (await client.DownloadStringTaskAsync(someUrl)).ToUpper();
});
*/