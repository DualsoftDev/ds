using System;
using Dsu.PLC.Common;

namespace Dsu.PLC.LS
{
    public class LsTag : TagBase
    {
        //    internal LogixTag UserTag { get; set; }
        //    internal LogixTagInfo TagInfo { get; set; }

        //    private LsConnection Connection => (LsConnection)ConnectionBase;
        //    private LogixProcessor Processor => Connection?.COMObject;


        //    public sealed override string Name
        //    {
        //        get { return TagInfo.TagName; }
        //        set { TagInfo = Processor.GetTagInformation(value); }
        //    }

        public override bool IsBitAddress { get { throw new NotImplementedException(); } protected internal set { throw new NotImplementedException(); } }

        public LsTag(LsConnection connection, string name)
            : base(connection)
        {
            Name = name;
            //UserTag = LogixTagFactory.CreateTag(name, connection.COMObject);
            connection?.AddMonitoringTag(this);
        }
    }
}
