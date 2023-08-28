using Server.HW.Common;
using Server.HW.WMX3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

internal class WMXChannelRequestExecutor : ChannelRequestExecutor
{
    public WMXConnection WMXConnection { get { return (WMXConnection)Connection; } }
    private Dictionary<int, WMXTag> _WMXInBitTags = new Dictionary<int, WMXTag>();
    private Dictionary<int, WMXTag> _WMXOutBitTags = new Dictionary<int, WMXTag>();

    public WMXChannelRequestExecutor(WMXConnection connection, IEnumerable<TagHW> tags)
        : base(connection, tags)
    {
        var wmxTags = tags.Cast<WMXTag>().Where(w => w.DataType == TagDataType.Bool);

        _WMXInBitTags = wmxTags
            .Where(w => w.IOType == TagIOType.Input)
            .ToDictionary(s => s.Address, s => s);

        _WMXOutBitTags = wmxTags
            .Where(w => w.IOType == TagIOType.Output)
            .ToDictionary(s => s.Address, s => s);
    }


    public override bool ExecuteRead()
    {
        excuteReadInputs();
        excuteReadOutputs();
        excuteWriteOutputs();
        return true;
    }

    public void excuteReadInputs()
    {
        var inData = WMXConnection.InData;
        var oldData = inData.ToList().ToArray();

        WMXConnection.WMX3Lib_Io.GetInBytes(0, inData.Length, ref inData);
        UpdateIO(inData, oldData, true);
    }
    public void excuteReadOutputs()
    {
        var outData = WMXConnection.OutData;
        var oldData = outData.ToList().ToArray();

        WMXConnection.WMX3Lib_Io.GetOutBytes(0, outData.Length, ref outData);
        UpdateIO(outData, oldData, false);
    }

    public void excuteWriteOutputs()
    {
        foreach (var outTag in _WMXOutBitTags.Values)
        {
            if (outTag.Value != outTag.WriteRequestValue)
            {
                outTag.Value = outTag.WriteRequestValue;
                WMXConnection.WMX3Lib_Io.SetOutBit(outTag.ByteOffset, outTag.BitOffset, Convert.ToByte(outTag.Value));
            }
        }
    }

    private void UpdateIO(byte[] newData, byte[] oldData, bool bInput)
    {
        for (int iByte = 0; iByte < newData.Length; iByte++)
        {
            if (newData[iByte] == oldData[iByte])
                continue;

            var oldBits = new BitArray(oldData[iByte]);
            var newBits = new BitArray(newData[iByte]);

            for (int iBit = 0; iBit < newBits.Length; iBit++)
                if (oldBits[iBit] != newBits[iBit])
                {
                    if (bInput)
                        _WMXInBitTags[iByte * 8 + iBit].Value = newBits[iBit];
                    else
                        _WMXOutBitTags[iByte * 8 + iBit].Value = newBits[iBit];
                }
        }
    }
}
