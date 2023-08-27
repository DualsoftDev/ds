using System;

namespace Server.HW.Common
{
    public interface IConnection : IDisposable
    {
        IConnectionParameters ConnectionParameters { get; /*set;*/ }
        bool Connect();
        bool Disconnect();

        object ReadATag(ITagHW tag);
    }
}
