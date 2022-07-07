using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsu.Common.Utilities.Core
{
    /// <summary>
    /// System.Reactive 를 사용할 수 있는 환경에서는, System.Reactive.Disposable.Create(..) 를 사용하면 OK
    /// </summary>
    // https://elliotbalynn.blog/2014/10/27/anonymous-disposable-adapter-that-makes-any-object-idisposable/
    public static class Diposable
    {
        public static IDisposable Create(Action action)
        {
            return new AnonymousDisposable(action);
        }
        private struct AnonymousDisposable : IDisposable
        {
            private readonly Action _dispose;
            public AnonymousDisposable(Action dispose)
            {
                _dispose = dispose;
            }
            public void Dispose()
            {
                if (_dispose != null)
                {
                    _dispose();
                }
            }
        }
    }
}
