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

    public XG5KChannelRequestExecutor(XG5KConnection connection, IEnumerable<TagHW> tags)
        : base(connection, tags)
    {
    }

    public override bool ExecuteRead()
    {
        return true;
    }

   
}
