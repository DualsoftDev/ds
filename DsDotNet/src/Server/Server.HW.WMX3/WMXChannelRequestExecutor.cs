using Server.HW.Common;
using Server.HW.WMX3;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

internal class WMXChannelRequestExecutor : ChannelRequestExecutor
{
    public WMXConnection WMXConnection { get { return (WMXConnection)Connection; } }
    private Dictionary<int, WMXTag> _WMXInBitTags = new Dictionary<int, WMXTag>();
    private Dictionary<int, WMXTag> _WMXOutBitTags = new Dictionary<int, WMXTag>();
    private Dictionary<int, WMXTag> _LSMemoryBitTags = new Dictionary<int, WMXTag>();

    public WMXChannelRequestExecutor(WMXConnection connection, IEnumerable<TagHW> tags)
        : base(connection, tags)
    {
        var wmxTags = tags.Cast<WMXTag>().Where(w => w.DataType == TagDataType.Bool);
        wmxTags.ToList().ForEach(f =>
        {
            if (f.DataType == TagDataType.Bool)
                f.Value = false;
            else
                f.Value = 0;
        });
        connection.ClearData();

        _WMXInBitTags = wmxTags
            .Where(w => w.IOType == TagIOType.Input)
            .ToDictionary(s => s.GetBitIndex(), s => s);

        _WMXOutBitTags = wmxTags
            .Where(w => w.IOType == TagIOType.Output)
            .ToDictionary(s => s.GetBitIndex(), s => s);
        
        _LSMemoryBitTags = wmxTags
             .Where(w => w.IOType == TagIOType.Memory)
             .ToDictionary(s => s.GetBitIndex(), s => s);
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
        var outputNMemory = _WMXOutBitTags.Values.ToList();
        outputNMemory.AddRange(_LSMemoryBitTags.Values);

        foreach (var outTag in outputNMemory)
        {
            if (outTag.WriteRequestValue == null) continue;
            if (outTag.Value != outTag.WriteRequestValue)
            {
                outTag.Value = outTag.WriteRequestValue;
                WMXConnection.WMX3Lib_Io.SetOutBit(outTag.ByteOffset, outTag.BitOffset, Convert.ToByte(outTag.WriteRequestValue));
            }
        }
    }

    private void UpdateIO(byte[] newData, byte[] oldData, bool bInput)
    {
        for (int iByte = 0; iByte < newData.Length; iByte++)
        {
            if (newData[iByte] == oldData[iByte])
                continue;

            if (bInput)
                WMXConnection.InData[iByte] = newData[iByte];
            else
                WMXConnection.OutData[iByte] = newData[iByte];

            var oldBits = new BitArray(new byte[] { oldData[iByte] });
            var newBits = new BitArray(new byte[] { newData[iByte] });

            for (int iBit = 0; iBit < newBits.Length; iBit++)
                if (oldBits[iBit] != newBits[iBit])
                {
                    var index = iByte * 8 + iBit;
                    var value = newBits[iBit];
                    if (bInput)
                    {
                        if (_WMXInBitTags.ContainsKey(index))
                            _WMXInBitTags[index].Value = value;
                    }
                    else
                    {
                        if (_WMXOutBitTags.ContainsKey(index))
                            _WMXOutBitTags[index].Value = value;
                    }
                }
        }
    }
}
