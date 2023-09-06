using Server.HW.Common;
using System;
using System.Windows.Media;

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
    public sealed override string Name
    {
        get { return base.Name; }
        set { base.Name = value; }
    }
    public string Address { get; private set; } = string.Empty;

    public sealed override object Value
    {
        get { return base.Value; }
        set { base.Value = value; }
    }

    public void SetAddress(string name)
    {
        var upperName = name.ToUpper().Trim();

        if (upperName.StartsWith("I") || upperName.StartsWith("%IX"))
            this.IOType = TagIOType.Input;
        else if (upperName.StartsWith("O") || upperName.StartsWith("%QX"))
            this.IOType = TagIOType.Output;
        else if (upperName.StartsWith("M") || upperName.StartsWith("%MX"))
            this.IOType = TagIOType.Memory;
        else
            throw new HWExceptionTag("Address Head Type Error");

        string byteBit = "0.0";
        if (upperName.StartsWith("%")) //IEC 규격입력 일단 1CPU만 %IX0.
        {

            if (upperName.Contains("."))
            {
                if (upperName.Split('.').Length != 3)
                    throw new HWExceptionTag("WMXTag IEC type %[Device][CPU].[SLOT].[ID] ex %IX0.12.4, %MX1");
                else
                    byteBit = upperName.Replace("%IX0.", "").Replace("%QX0.", "");
            }
            else
            {
                var bitIndex = Convert.ToInt32(upperName.Replace("%MX", ""));
                byteBit = $"{bitIndex/8}.{bitIndex%8}";
            }
        }
        else
        {
            if (upperName.Split('.').Length != 2)
                throw new HWExceptionTag("WMXTag type [Device][Byte].[Bit] ex I12.4, M0.0");

            byteBit = upperName.TrimStart('I').TrimStart('O').TrimStart('M');
        }

        Address = upperName;
        ByteOffset = Convert.ToInt32(byteBit.Split('.')[0]);
        BitOffset = Convert.ToInt32(byteBit.Split('.')[1]);
    }
    public int GetBitIndex() => ByteOffset * 8 + BitOffset;

}
