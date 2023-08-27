using Server.HW.Common;
using Server.HW.WMX3;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

internal class WMXChannelRequestExecutor : ChannelRequestExecutor
{
	public WMXConnection WMXConnection { get { return (WMXConnection)Connection; } }
	internal IEnumerable<TagHW> WMXTags => Tags.OfType<TagHW>();

    public WMXChannelRequestExecutor(WMXConnection connection, IEnumerable<TagHW> tags)
		: base(connection, tags)
	{
	}

	public override bool ExecuteRead()
	{
        var inData = WMXConnection.InData;
        var oldData = inData.ToList();

        WMXConnection.WMX3Lib_Io.GetInBytes(0, inData.Length, ref inData);

        for (int iByte = 0; iByte < inData.Length; iByte++)
        {
            if (inData[iByte] == oldData[iByte])
                continue;
            var oldBits = new BitArray(oldData[iByte]);
            var newBits = new BitArray(inData[iByte]);

            for (int iBit = 0; iBit < newBits.Length; iBit++)
                if (oldBits[iBit] != newBits[iBit])
                    HWEvent.ValueChangeSubjectPaixInputs.OnNext(Tuple.Create(iBit, newBits[iBit]));
        }


        return true;
	}
}
