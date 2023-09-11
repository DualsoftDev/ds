using DsXgComm.Monitoring;
using Dual.PLC.Common;
using FSharpPlus.Control;
using Server.HW.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using WMX3ApiCLR;
using XGCommLib;
using static DsXgComm.Connect;
using ChannelRequestExecutor = Server.HW.Common.ChannelRequestExecutor;
using ConnectionBase = Server.HW.Common.ConnectionBase;
using IConnectionParameters = Server.HW.Common.IConnectionParameters;

namespace Server.HW.WMX3;

public class WMXConnection : ConnectionBase
{
    private Io _wmx3Lib_Io;
    private WMX3Api _wmx3Lib;
    public byte[] InData { get; private set; }
    public byte[] OutData { get; private set; }
    private int _InCnt;
    private int _OutCnt;
    private string _Ip;
    internal IEnumerable<WMXTag> WMXTags => Tags.Values.OfType<WMXTag>();
    internal Io WMX3Lib_Io => _wmx3Lib_Io;

    private WMXConnectionParameters _connectionParameters;
    public DsXgConnection ConnLS { get; private set; }

    public WMXConnection(WMXConnectionParameters parameters, int numIn, int numOut)
        : base(parameters)
    {
        _wmx3Lib = new WMX3Api();
        _wmx3Lib_Io = new Io(_wmx3Lib);
        _InCnt = numIn;
        _OutCnt = numOut;
        _connectionParameters = parameters;
        PerRequestDelay = (int)parameters.TimeoutScan.TotalMilliseconds;
        InData = Enumerable.Repeat((byte)0, count: _InCnt).ToArray();
        OutData = Enumerable.Repeat((byte)0, count: _OutCnt).ToArray();
        _Ip = parameters.IP;
        ConnLS = new DsXgConnection();
    }

    public override IConnectionParameters ConnectionParameters
    {
        get { return _connectionParameters; }
        set { _connectionParameters = (WMXConnectionParameters)value; }
    }

    public uint TimeoutConnecting => (uint)_connectionParameters.TimeoutConnecting.TotalMilliseconds;

    private bool _IsConnected;

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
            if (!IsCreatedDevice)
            {
                var ret = _wmx3Lib.CreateDevice("C:\\Program Files\\SoftServo\\WMX3\\",//설치 PC만 사용 가능  ip설정 필요없음
                    DeviceType.DeviceTypeNormal, TimeoutConnecting);

                IsCreatedDevice = ret == 0;
            }
            if (IsCreatedDevice)
            {
                EngineStatus _enStatus = new EngineStatus();
                _IsConnected = SpinWait.SpinUntil(() =>
                {
                    _wmx3Lib.StartCommunication(TimeoutConnecting);
                    _wmx3Lib.GetEngineStatus(ref _enStatus);
                    return _enStatus.State == EngineState.Communicating;

                }, _connectionParameters.TimeoutConnecting);
            }

            return _IsConnected;
        }
        catch (Exception)
        {
            return false;
        }
    }


    protected override void Dispose(bool disposing)
    {
        // Stop Communication.      //잘못된 메모리 참조 있음
        //_wmx3Lib?.StopCommunication(TimeoutConnecting);
        // Discard the device.
        _wmx3Lib?.CloseDevice();
        _wmx3Lib_Io?.Dispose();
        _wmx3Lib?.Dispose();
        ConnLS.Disconnect(); 
        base.Dispose(disposing);
    }


    public override TagHW CreateTag(string name) => new WMXTag(this, name);

    public override IEnumerable<ChannelRequestExecutor> Channelize(IEnumerable<TagHW> tags)
    {
        var channel = new WMXChannelRequestExecutor(this, tags);
        yield return channel;
    }


    public override object ReadATag(ITagHW tag) => throw new NotImplementedException();
}
