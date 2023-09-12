using DsXgComm;
using Dual.PLC.Common;
using FSharpPlus.Control;
using Server.HW.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using XGCommLib;
using static DsXgComm.Connect;
using ChannelRequestExecutor = Server.HW.Common.ChannelRequestExecutor;
using ConnectionBase = Server.HW.Common.ConnectionBase;
using IConnectionParameters = Server.HW.Common.IConnectionParameters;

namespace Server.HW.XG5K;

public class XG5KConnection : ConnectionBase
{
    public byte[] InData { get; private set; }
    public byte[] OutData { get; private set; }
    private int _InCnt;
    private int _OutCnt;
    private string _Ip;
    internal IEnumerable<XG5KTag> XG5KTags => Tags.Values.OfType<XG5KTag>();

    private XG5KConnectionParameters _connectionParameters;
    public DsXgConnection ConnLS { get; private set; }

    public XG5KConnection(XG5KConnectionParameters parameters, int numIn, int numOut)
        : base(parameters)
    {
        _InCnt = numIn;
        _OutCnt = numOut;
        _connectionParameters = parameters;
        PerRequestDelay = (int)parameters.TimeoutScan.TotalMilliseconds;

        InData = Enumerable.Repeat((byte)0, count: _InCnt).ToArray();
        OutData = Enumerable.Repeat((byte)0, count: _OutCnt).ToArray();

        ClearData();
        _Ip = parameters.IP;
        ConnLS = new DsXgConnection();
    }

    public override IConnectionParameters ConnectionParameters
    {
        get { return _connectionParameters; }
        set { _connectionParameters = (XG5KConnectionParameters)value; }
    }

    public uint TimeoutConnecting => (uint)_connectionParameters.TimeoutConnecting.TotalMilliseconds;

    private bool _IsConnected = false;

    public override bool IsConnected { get { return _IsConnected; } }
    public bool IsCreatedDevice { get; private set; }
    public void ClearData()
    {
        InData = Enumerable.Repeat((byte)0, count: _InCnt).ToArray();
        OutData = Enumerable.Repeat((byte)0, count: _OutCnt).ToArray();
    }

    public override bool Connect()
    {
        if (_IsConnected)
            return true;

        try
        {
            return ConnLS.Connect(_Ip + ":2004");
        }
        catch (Exception)
        {
            return false;
        }
    }


    protected override void Dispose(bool disposing)
    {
        ConnLS.Disconnect(); 
        base.Dispose(disposing);
    }


    public override TagHW CreateTag(string name) => new XG5KTag(this, name);

    public override IEnumerable<ChannelRequestExecutor> Channelize(IEnumerable<TagHW> tags)
    {
        var channel = new XG5KChannelRequestExecutor(this, tags);
        yield return channel;
    }


    public override object ReadATag(ITagHW tag) => throw new NotImplementedException();
}
