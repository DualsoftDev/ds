using System;

namespace Dsu.Common.Utilities
{
    /// <summary>
    /// Rx event 에 대한 subscribe 가 가능한 객체가 구현해야 할 interface
    /// Dispose 시에 모든 subscription 을 dispose 해야 한다.
    /// </summary>
    public interface ISubscribable : IDisposable
    {
        /* Usage sample
            private List<IDisposable> _subscriptions = new List<IDisposable>();
            public void AddSubscription(IDisposable subscription) {  _subscriptions.Add(subscription); }

            // Requires disposal of subscriptions on Dispose() method 
         * 
         */
        void AddSubscription(IDisposable subscription);
    }
}
