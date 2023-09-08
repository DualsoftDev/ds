using LanguageExt.ClassInstances.Pred;
using Server.HW.Common;
using Server.HW.XG5K;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

internal class XG5KChannelRequestExecutor : ChannelRequestExecutor
{
    public XG5KConnection XG5KConnection { get { return (XG5KConnection)Connection; } }
    private Dictionary<int, XG5KTag> _XG5KInBitTags = new Dictionary<int, XG5KTag>();
    private Dictionary<int, XG5KTag> _XG5KOutBitTags = new Dictionary<int, XG5KTag>();
    private Dictionary<int, XG5KTag> _LSMemoryBitTags = new Dictionary<int, XG5KTag>();

    public XG5KChannelRequestExecutor(XG5KConnection connection, IEnumerable<TagHW> tags)
        : base(connection, tags)
    {
        var wmxTags = tags.Cast<XG5KTag>().Where(w => w.DataType == TagDataType.Bool);
        wmxTags.ToList().ForEach(f =>
        {
            if (f.DataType == TagDataType.Bool)
                f.Value = false;
            else
                f.Value = 0;
        });
        connection.ClearData();

        _XG5KInBitTags = wmxTags
            .Where(w => w.IOType == TagIOType.Input)
            .ToDictionary(s => s.GetBitIndex(), s => s);

        _XG5KOutBitTags = wmxTags
            .Where(w => w.IOType == TagIOType.Output)
            .ToDictionary(s => s.GetBitIndex(), s => s);
        
        _LSMemoryBitTags = wmxTags
             .Where(w => w.IOType == TagIOType.Memory)
             .ToDictionary(s => s.GetBitIndex(), s => s);
    }


    public override bool ExecuteRead()
    {
        excuteReadInputs();
        //excuteReadOutputs();
        excuteWriteOutputs();
        return true;
    }

    public void excuteReadInputs()
    {
        var inData = XG5KConnection.InData;
        var oldData = inData.ToList().ToArray();

        var ret =  XG5KConnection.ConnLS.ReadBit('I');

        for (int i = 0; i < inData.Length; i++)
        {
            inData[i] = ret[i];
        }
     
        //XG5KConnection.XG5KLib_Io.GetInBytes(0, inData.Length, ref inData);

        UpdateIO(inData, oldData, true);
    }
    public void excuteReadOutputs()
    {
        var outData = XG5KConnection.OutData;
        var oldData = outData.ToList().ToArray();

        //XG5KConnection.XG5KLib_Io.GetOutBytes(0, outData.Length, ref outData);
        UpdateIO(outData, oldData, false);
    }

    public void excuteWriteOutputs()
    {
        var outputNMemory = _XG5KOutBitTags.Values.ToList();
        outputNMemory.AddRange(_LSMemoryBitTags.Values);

        foreach (var outTag in outputNMemory)
        {
            if (outTag.WriteRequestValue == null) continue;
            if (outTag.Value != outTag.WriteRequestValue)
            {
                outTag.Value = outTag.WriteRequestValue;
                if (outTag.Address.StartsWith("%MX"))
                    XG5KConnection.ConnLS.WriteBit("M", outTag.GetBitIndex(), Convert.ToInt32(outTag.WriteRequestValue));
                if (outTag.Address.StartsWith("%QX"))
                    XG5KConnection.ConnLS.WriteBit("Q", outTag.GetBitIndex(), Convert.ToInt32(outTag.WriteRequestValue));
                //else
                //            XG5KConnection.XG5KLib_Io.SetOutBit(outTag.ByteOffset, outTag.BitOffset, Convert.ToByte(outTag.WriteRequestValue));
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
                XG5KConnection.InData[iByte] = newData[iByte];
            else
                XG5KConnection.OutData[iByte] = newData[iByte];

            var oldBits = new BitArray(new byte[] { oldData[iByte] });
            var newBits = new BitArray(new byte[] { newData[iByte] });

            for (int iBit = 0; iBit < newBits.Length; iBit++)
                if (oldBits[iBit] != newBits[iBit])
                {
                    var index = iByte * 8 + iBit;
                    var value = newBits[iBit];
                    if (bInput)
                    {
                        if (_XG5KInBitTags.ContainsKey(index))
                            _XG5KInBitTags[index].Value = value;
                    }
                    else
                    {
                        if (_XG5KOutBitTags.ContainsKey(index))
                            _XG5KOutBitTags[index].Value = value;
                    }
                }
        }
    }
}
