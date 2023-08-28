using Server.HW.Common;

namespace Server.HW.WMX3;
public class WMXTag : TagHW
{
    private WMXConnection Connection => (WMXConnection)ConnectionBase;
    public WMXTag(WMXConnection connection, string name)
        : base(connection)
    {
        Name = name;
        connection.AddMonitoringTag(this);

    }
    public int Address { get; private set; }
    public void SetAddress(int offsetBit)
    {
        Address = offsetBit;
        ByteOffset = offsetBit / 8;
        BitOffset = offsetBit % 8;
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


}
