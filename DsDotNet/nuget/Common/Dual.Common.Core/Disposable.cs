using System;

namespace Dual.Common.Core
{
    /// <summary>
    /// System.Reactive 를 사용할 수 있는 환경에서는, System.Reactive.Disposable.Create(..) 를 사용하면 OK
    /// <br/> - F# 사용 가능환경에서는 disposable {} computation builder 사용 권장
    /// </summary>
    // https://elliotbalynn.blog/2014/10/27/anonymous-disposable-adapter-that-makes-any-object-idisposable/
    public static class Diposable
    {
        /// <summary>
        /// IDiposable 객체 생성해서 반환
        /// <br/> - System.Reactive 를 사용할 수 있는 환경에서는, System.Reactive.Disposable.Create(..) 를 사용하면 OK
        /// <br/> - F# 사용 가능환경에서는 disposable {} computation builder 사용 권장
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
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
