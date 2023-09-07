using Server.HW.Common;
using System;
//using System.Windows.Media;

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
    public string Address { get; private set; } = string.Empty;
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

    public void SetAddress(string name)
    {
        var upperName = name.ToUpper().Trim();

        if (upperName.StartsWith("I"))
            this.IOType = TagIOType.Input;
        else if (upperName.StartsWith("O"))
            this.IOType = TagIOType.Output;
        else if (upperName.StartsWith("M"))
            this.IOType = TagIOType.Memory;
        else
            throw new HWExceptionTag("Address Head Type Error");

        if (upperName.Split('.').Length != 2)
            throw new HWExceptionTag("WMXTag type [Device][Byte].[Bit] ex I12.4, M0.0");

        var byteBit = upperName.TrimStart('I').TrimStart('O').TrimStart('M');

        Address = upperName;
        ByteOffset = Convert.ToInt32(byteBit.Split('.')[0]);
        BitOffset = Convert.ToInt32(byteBit.Split('.')[1]);
    }
    public int GetBitIndex() => ByteOffset * 8 + BitOffset;

}
