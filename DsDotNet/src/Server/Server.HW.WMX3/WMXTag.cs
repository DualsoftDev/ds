using System;
using Server.HW.Common;

namespace Server.HW.WMX3;
public class WMXTag : TagHW
{

    private WMXConnection Connection => (WMXConnection)ConnectionBase;

    public WMXTag(WMXConnection connection, string name)
        : base(connection)
    {
        Name = name;
        connection?.AddMonitoringTag(this);
    }


    public sealed override string Name
    {
        get { return this.Name; }
        set { this.Name = value; }
    }


    public sealed override object Value
    {
        get { return this.Value; }
        set
        {
            this.Value = value;
            base.Value = value;
        }
    }


}
