using System;

namespace Dsu.PLC
{
    public interface IConnection : IDisposable
    {
        ICpu Cpu { get; }
        IConnectionParameters ConnectionParameters { get; /*set;*/ }
        bool Connect();
        bool Disconnect();

        object ReadATag(ITag tag);
    }
}
