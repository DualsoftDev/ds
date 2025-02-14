using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;
using log4net;

namespace Dual.PLC.Common
{
    /// <summary>
    /// Common base class for PLC connection
    /// </summary>
    public abstract class ConnectionBase : IConnection
    {
        public static ILog Logger = Global.Logger;
        public abstract ICpu Cpu { get; }
        public virtual IConnectionParameters ConnectionParameters { get; set; }
        public bool IsUSBConnection => ConnectionParameters is IConnectionParametersUSB;
        public bool IsEthernetConnection => ConnectionParameters is IConnectionParametersEthernet;

        public Dictionary<string, TagBase> Tags { get; } = new Dictionary<string, TagBase>();


        internal List<ChannelRequestExecutor> _channels;

        ///  Data Exchange Cancellation token source
        protected CancellationTokenSource DataExchangeCts { get; set; }

        /// <summary>
        /// Delay in milliseconds between PLC requests
        /// </summary>
        public int PerRequestDelay { get; set; } = 1000;

        public abstract bool Connect();

        public virtual bool Disconnect()
        {
            Dispose();
            return true;
        }

        public virtual void ReconnectOnDemand(Exception ex)
        {
            Logger?.Warn("Reconnecting on demand.");
            DataExchangeCts.Cancel();
            Disconnect();
            DataExchangeCts = new CancellationTokenSource();
            Connect();
        }

        public abstract TagBase CreateTag(string name);
        public Try<TagBase> TryCreateTag(string name) => () => CreateTag(name);

        public IEnumerable<Try<TagBase>> TryCreateTags(IEnumerable<string> tagNames)
        {
            return from tname in tagNames
                select TryCreateTag(tname);
        }

        public IEnumerable<TagBase> CreateTags(IEnumerable<string> tagNames)
        {
            var tags = from trial in TryCreateTags(tagNames)
                       select match(trial,
                    Succ: v => v,
                    Fail: ex =>
                    {
                        Trace.WriteLine($"Exception: {ex}");
                        return null;
                    }
                );

            return tags.ToArray();
        }


        public abstract object ReadATag(ITag tag);

        public virtual void WriteATag(ITag tag) { throw new NotImplementedException("Not yet implemented."); }
        public virtual void WriteTags(IEnumerable<ITag> tags) { throw new NotImplementedException("Not yet implemented."); }

        public Subject<IObservableEvent> Subject { get; } = new Subject<IObservableEvent>();
        //protected List<IDisposable> _subscriptions = new List<IDisposable>();
        //public void AddSubscription(IDisposable subscription) { _subscriptions.Add(subscription); }

        public ConnectionBase(IConnectionParameters parameters)
        {
            ConnectionParameters = parameters;
            Logger?.Debug("ConnectionBase()");

            //AddSubscription(Subject.OfType<TagAddEvent>().Subscribe(evt =>
            //{
            //    var tag = evt.Tag;
            //    _tags.Add(tag.Name, tag);
            //}));

            //AddSubscription(Subject.OfType<TagsAddEvent>().Subscribe(evt =>
            //{
            //    var tag = evt.Tags;
            //    _tags.Add(tag.Name, tag);
            //}));

        }

        public virtual bool AddMonitoringTag(TagBase tag)
        {
            if (tag == null || Tags.ContainsKey(tag.Name))
            {
                Logger?.Debug($"Tag {tag.Name} already exists in connection {ConnectionParameters}");
                return false;
            }

            Tags.Add(tag.Name, tag);
            Subject.OnNext(new TagAddEvent(tag));
            return true;
        }
        public virtual bool AddMonitoringTags(IEnumerable<TagBase> tags)
        {
            var arr = tags.Select(t => AddMonitoringTag(t)).ToArray();
            return arr.ForAll(identity);
        }

        public virtual void ResetMonitoringTags(IEnumerable<TagBase> tags)
        {
            Tags.Clear();
            foreach(var t in tags)
            {
                Tags.Add(t.Name, t);
                Subject.OnNext(new TagsAddEvent(tags));
            }

            PrepareDataExchangeLoop();
        }

        public void ClearTagNRestart()
        {
            Tags.Clear();
          
            PrepareDataExchangeLoop();
        }


        public abstract IEnumerable<ChannelRequestExecutor> Channelize(IEnumerable<TagBase> tags);

        public virtual void PrepareDataExchangeLoop()
        {
            _channels = Channelize(Tags.Values).ToList();
        }

        /// <summary>
        /// Batch read registered tags
        /// 읽을 때, 발생한 오류들을 exception list 로 반환
        /// </summary>
        public virtual IEnumerable<Exception> ReadAllChannels()
        {
            var query =
                (
                    from ch in _channels
                    select match(ch.TryExecuteRead(),
                        Succ: v => null,
                        Fail: ex => ex)
                ).ToArray();

            return query.Where(ex => ex != null).ToArray();
        }


        public virtual IEnumerable<Exception> WriteAllChannels() { yield break; }

        public IEnumerable<Exception> SingleScan(bool prepare=false)
        {
            Logger?.Debug("Starting SingleScan()");
            if (prepare)
                PrepareDataExchangeLoop();

            return ReadAllChannels()
                .Concat(WriteAllChannels())
                ;
        }

        public virtual async Task StartDataExchangeLoopAsync()
        {
            Logger?.Debug("Starting StartDataExchangeLoopAsync()");
            PrepareDataExchangeLoop();
            DataExchangeCts = new CancellationTokenSource();
            await Task.Run(async () =>
            {
                while (DataExchangeCts != null && ! DataExchangeCts.IsCancellationRequested )
                {
                    await Task.Delay(PerRequestDelay);
                    //Trace.WriteLine("Monitoring...");

                    try
                    {
                        var exceptions = SingleScan();
                        if (exceptions.Any())
                        {
                            Disconnect();
                            Connect();
                        }
                    }catch (Exception ex)
                    {
                        ex.Data.Clear();
                    }

                    
                    //ReconnectOnDemand(exceptions.First());
                }
            }, DataExchangeCts.Token);
        }

        public void StopDataExchangeLoop()
        {
            DataExchangeCts.Cancel();
            DataExchangeCts.Dispose();
            DataExchangeCts = null;
        }




        private bool _disposed;

        /// <summary>
        /// Finalizer 에 의한 호출.  reference counter 값이 0 된 후에, GC 에 의해서 호출됨
        /// </summary>
        ~ConnectionBase()
        {
            Dispose(false);     // false : 암시적 dispose 호출
        }
        public void Dispose()
        {
            Dispose(true);      // true : 명시적 dispose 호출
            GC.SuppressFinalize(this);  // 사용자에 의해서 명시적으로 dispose 되었으므로, GC 는 이 객체에 대해서 손대지 말것을 알림.
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing">명시적(using() 구문 사용 포함) Dispose() 호출시 true, Finalizer 에서 암시적으로 호출시 false</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                /*
                 * free other managed objects that implement IDisposable only
                 */
                //_stream.Dispose();
            }

            /*
             * release any unmanaged objects
             * set the object references to null
             */
            _disposed = true;
        }
    }
}
