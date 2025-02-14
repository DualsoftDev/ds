using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
    public static class EmCancellationToken
    {
        /// 이미 존재하는 cancellation token 에 지정된 시간 이후에 cancel 하는 time out 기능을 부가한 token source 를 생성해서 반환한다.
        public static CancellationTokenSource CreateLinkedTimeoutTokenSource(this CancellationToken token, int delayMilli)
        {
            CancellationTokenSource ctsTimeOut = new CancellationTokenSource();
            ctsTimeOut.CancelAfter(delayMilli);
            return CancellationTokenSource.CreateLinkedTokenSource(token, ctsTimeOut.Token);
        }
        /// 이미 존재하는 cancellation token 에 지정된 시간 이후에 cancel 하는 time out 기능을 부가한 token source 를 생성해서 반환한다.
        public static CancellationTokenSource CreateLinkedTimeoutTokenSource(this CancellationToken token, TimeSpan ts) => CreateLinkedTimeoutTokenSource(token, (int)ts.TotalMilliseconds);
    }



    /// <summary>
    /// Implementation of PauseTokenSource pattern based on the blog post:
    /// https://bhrnjica.net/2014/04/15/pausing-and-cancelling-async-method-in-c/
    /// http://blogs.msdn.com/b/pfxteam/archive/2013/01/13/cooperatively-pausing-async-methods.aspx
    /// </summary>
    public class PauseTokenSource
    {

        private TaskCompletionSource<bool> m_paused;
        internal static readonly Task s_completedTask = Task.FromResult(true);

        public bool IsPaused
        {
            get { return m_paused != null; }
            set
            {
                if (value)
                {
                    Interlocked.CompareExchange(
                        ref m_paused, new TaskCompletionSource<bool>(), null);
                }
                else
                {
                    while (true)
                    {
                        var tcs = m_paused;
                        if (tcs == null) return;
                        if (Interlocked.CompareExchange(ref m_paused, null, tcs) == tcs)
                        {
                            tcs.SetResult(true);
                            break;
                        }
                    }
                }
            }
        }

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;

        public PauseToken Token => new PauseToken(this);

        internal Task WaitWhilePausedAsync()
        {
            var cur = m_paused;
            return cur != null ? cur.Task : s_completedTask;
        }
    }

    public struct PauseToken
    {
        private readonly PauseTokenSource m_source;
        internal PauseToken(PauseTokenSource source) { m_source = source; }

        public bool IsPaused => m_source != null && m_source.IsPaused;

        public Task WaitWhilePausedAsync()
        {
            return IsPaused ?
                m_source.WaitWhilePausedAsync() :
                PauseTokenSource.s_completedTask;
        }
    }
}
