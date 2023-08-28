using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Server.HW.Common
{
    /// <summary>
    /// Common base class for HW connection
    /// </summary>
    public abstract class ConnectionBase : IConnection
    {
        public virtual IConnectionParameters ConnectionParameters { get; set; }
        public bool IsUSBConnection => ConnectionParameters is IConnectionParametersUSB;
        public bool IsEthernetConnection => ConnectionParameters is IConnectionParametersEthernet;

        public Dictionary<string, TagHW> Tags { get; } = new Dictionary<string, TagHW>();


        internal List<ChannelRequestExecutor> _channels;

        ///  Data Exchange Cancellation token source
        protected CancellationTokenSource DataExchangeCts { get; set; }

        /// <summary>
        /// Delay in milliseconds between HW requests
        /// </summary>
        public int PerRequestDelay { get; set; } = 1000;
        public abstract bool IsConnected { get; }
        public bool IsRunning { get; private set; }
        public abstract bool Connect();

        public virtual bool Disconnect()
        {
            Dispose();
            return true;
        }

        public virtual void ReconnectOnDemand(Exception ex)
        {
            Trace.WriteLine("Reconnecting on demand.");
            DataExchangeCts.Cancel();
            Disconnect();
            DataExchangeCts = new CancellationTokenSource();
            Connect();
        }

        public abstract TagHW CreateTag(string name);
        public TagHW TryCreateTag(string name) =>  CreateTag(name);

        public IEnumerable<TagHW> TryCreateTags(IEnumerable<string> tagNames)
        {
            return from tname in tagNames
                   select TryCreateTag(tname);
        }

        public IEnumerable<TagHW> CreateTags(IEnumerable<string> tagNames)
        {
            var tags = TryCreateTags(tagNames);

            return tags.ToArray();
        }


        public abstract object ReadATag(ITagHW tag);

        public virtual void WriteATag(ITagHW tag) { throw new NotImplementedException("Not yet implemented."); }
        public virtual void WriteTags(IEnumerable<ITagHW> tags) { throw new NotImplementedException("Not yet implemented."); }

        public Subject<IObservableEvent> Subject { get; } = new Subject<IObservableEvent>();
        //protected List<IDisposable> _subscriptions = new List<IDisposable>();
        //public void AddSubscription(IDisposable subscription) { _subscriptions.Add(subscription); }

        public ConnectionBase(IConnectionParameters parameters)
        {
            ConnectionParameters = parameters;
            Trace.WriteLine("ConnectionBase()");

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

        public virtual bool AddMonitoringTag(TagHW tag)
        {
            if (tag == null || Tags.ContainsKey(tag.Name))
            {
                Trace.WriteLine($"Tag {tag.Name} already exists in connection {ConnectionParameters}");
                return false;
            }

            Tags.Add(tag.Name, tag);
            Subject.OnNext(new TagAddEvent(tag));
            return true;
        }
        public virtual bool AddMonitoringTags(IEnumerable<TagHW> tags)
        {
            var arr = tags.Select(t => AddMonitoringTag(t)).ToArray();
            return arr.Any(w => !w);
        }

        public virtual void ResetMonitoringTags(IEnumerable<TagHW> tags)
        {
            Tags.Clear();
            foreach (var t in tags)
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


        public abstract IEnumerable<ChannelRequestExecutor> Channelize(IEnumerable<TagHW> tags);

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
            try
            {
                foreach (var channel in _channels)
                {
                    channel.TryExecuteRead();
                }
                return new List<Exception>() { };
            }
            catch (Exception ex)
            {
                return new List<Exception> { ex };
            }
        }


        public virtual IEnumerable<Exception> WriteAllChannels() { yield break; }

        public IEnumerable<Exception> SingleScan(bool prepare = false)
        {
            Trace.WriteLine("Starting SingleScan()");
            if (prepare)
                PrepareDataExchangeLoop();

            return ReadAllChannels()
                .Concat(WriteAllChannels())
                ;
        }

        public virtual async Task StartDataExchangeLoopAsync()
        {
            Trace.WriteLine("Starting StartDataExchangeLoopAsync()");
            IsRunning = true;
            PrepareDataExchangeLoop();
            DataExchangeCts = new CancellationTokenSource();
            await Task.Run(async () =>
            {
                while (DataExchangeCts != null && !DataExchangeCts.IsCancellationRequested)
                {
                    await Task.Delay(PerRequestDelay);
                    Trace.WriteLine("Monitoring...");

                    var exceptions = SingleScan();
                    if (exceptions.Any())
                        ReconnectOnDemand(exceptions.First());
                }
            }, DataExchangeCts.Token);


        }

        public void StopDataExchangeLoop()
        {
            IsRunning = false;
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
