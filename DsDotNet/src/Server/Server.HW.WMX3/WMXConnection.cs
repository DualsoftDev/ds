using Server.HW.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WMX3ApiCLR;

namespace Server.HW.WMX3;

public class WMXConnection : ConnectionBase
{
    private Io _wmx3Lib_Io;
    private WMX3Api _wmx3Lib;
    public byte[] InData { get; private set; }
    public byte[] OutData { get; private set; }
    internal IEnumerable<WMXTag> WMXTags => Tags.Values.OfType<WMXTag>();
    internal Io WMX3Lib_Io => _wmx3Lib_Io;

    private WMXConnectionParameters _connectionParameters;

    public WMXConnection(WMXConnectionParameters parameters, int numIn, int numOut)
        : base(parameters)
    {
        _wmx3Lib = new WMX3Api();
        _wmx3Lib_Io = new Io(_wmx3Lib);
        InData = Enumerable.Repeat((byte)0, count: numIn).ToArray();
        OutData = Enumerable.Repeat((byte)0, count: numOut).ToArray();
        _connectionParameters = parameters;
        PerRequestDelay = (int)parameters.TimeoutScan.TotalMilliseconds;
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
        // Stop Communication.
        _wmx3Lib?.StopCommunication(TimeoutConnecting);
        // Discard the device.
        _wmx3Lib?.CloseDevice();
        _wmx3Lib_Io?.Dispose();
        _wmx3Lib?.Dispose();

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
