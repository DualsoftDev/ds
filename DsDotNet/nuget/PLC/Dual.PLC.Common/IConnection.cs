using System;

namespace Dual.PLC.Common
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
