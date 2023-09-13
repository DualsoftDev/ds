using Server.HW.Common;
using System;
using static DsXgComm.XGTagModule;

namespace Server.HW.XG5K;
public class XG5KTag : TagHW
{
    private XG5KConnection Connection => (XG5KConnection)ConnectionBase;
    public XG5KTag(XG5KConnection connection, string name)
        : base(connection)
    {
        Name = name;
    }
    public sealed override string Name
    {
        get { return base.Name; }
        set { base.Name = value; }
    }

    public sealed override object Value
    {
        get { return base.Value; }
        set { base.Value = value; }
    }

    
    public XgTagInfo XgPLCTag { get; set; }

    //test ahn 임시로 LS 64점에 맞춤
    public int GetBitIndex() => ByteOffset * 64 + BitOffset;

}
