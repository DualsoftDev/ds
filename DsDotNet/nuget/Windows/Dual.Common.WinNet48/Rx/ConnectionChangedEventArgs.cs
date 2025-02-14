using System.Diagnostics.Contracts;
using Dsu.Common.Interfaces;

namespace Dsu.Common.Utilities
{
    public class ConnectionChangedEventArgs : IObservableEvent
    {
        /// <summary> server 와의 connection 의 추가/삭제 여부.   false 이면 삭제를 의미함 </summary>
        public bool IsConnectionAdded { get; private set; }
        protected object Partner { get; private set; }
        public ConnectionChangedEventArgs(object partner, bool added)
        {
            Contract.Requires(partner is IServer || partner is IClient);
            Partner = partner;
            IsConnectionAdded = added;
        }
    }
    /// <summary>
    /// Server 와의 connection 이 변경될 때에 사용하는 event argument
    /// </summary>
    public class ServerConnectionChangedEventArgs : ConnectionChangedEventArgs
    {
        /// <summary> server </summary>
        public IServer Server { get { return Partner as IServer; } }

        /// <summary>
        /// connection 변경 argument 생성자
        /// </summary>
        /// <param name="server"></param>
        /// <param name="added">server 와의 connection 의 추가/삭제 여부.   false 이면 삭제를 의미함. </param>
        public ServerConnectionChangedEventArgs(IServer server, bool added)
            : base(server, added)
        {
        }
    }

    /// <summary>
    /// Abnormal termination
    /// </summary>
    public class ServerConnectionLostEventArgs : ServerConnectionChangedEventArgs
    {
        public ServerConnectionLostEventArgs(IServer server)
            : base(server, false)
        {
        }
    }


    public class ClientConnectionChangedEventArgs : ConnectionChangedEventArgs
    {
        public IClient Client { get { return Partner as IClient; } }

        public ClientConnectionChangedEventArgs(IClient client, bool added)
            : base(client, added)
        {
        }
    }

}