using DsXgComm;
using DsXgComm.Monitoring;
using Dual.PLC.Common;
using FSharpPlus.Control;
using Server.HW.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static DsXgComm.Connect;
using ChannelRequestExecutor = Server.HW.Common.ChannelRequestExecutor;
using ConnectionBase = Server.HW.Common.ConnectionBase;
using IConnectionParameters = Server.HW.Common.IConnectionParameters;
using System.Net.NetworkInformation;
using TagValueChangedEvent = Server.HW.Common.TagValueChangedEvent;
using LanguageExt;
using static DsXgComm.XGTagModule;

namespace Server.HW.XG5K;

public class XG5KConnection : ConnectionBase
{
    private int _InCnt;
    private int _OutCnt;
    private string _Ip;

    private XG5KConnectionParameters _connectionParameters;
    private XGTConnection _cpu = null;

    public List<XgTagInfo> XgTagInfos = new List<XgTagInfo>();  

    public XG5KConnection(XG5KConnectionParameters parameters, int scanDelay,  int numIn, int numOut)
        : base(parameters)
    {
        _InCnt = numIn;
        _OutCnt = numOut;
        _connectionParameters = parameters;
        PerRequestDelay = scanDelay;
        _Ip = parameters.Ip + (parameters.Port != 0 ? $":{parameters.Port}" : "");
    }

    public override IConnectionParameters ConnectionParameters
    {
        get { return _connectionParameters; }
        set { _connectionParameters = (XG5KConnectionParameters)value; }
    }

    public uint TimeoutConnecting => (uint)_connectionParameters.Timeout.TotalMilliseconds;

    private bool _IsConnected = false;

    public override bool IsConnected { get { return _IsConnected; } }
    public bool IsCreatedDevice { get; private set; }
    private static IDisposable DisposablePLCTagSubject;

    public void Stop() => _cpu?.ScanStop();
    public bool Start()
    {
        if (!_IsConnected)
            return false;


        Task.Run(() =>
        {
          
            if (DisposablePLCTagSubject != null) DisposablePLCTagSubject.Dispose(); 
            
            var addressDic = Tags.Values.ToDictionary(s => s.Address, d => d);
            DisposablePLCTagSubject = XGTagModule.PLCTagSubject.Subscribe(x =>
            {
                var tag = addressDic[x.Tag];
                tag.Value = x.Value;
                Trace.WriteLine($"{x.Tag} => {x.Value}");
            });

            _cpu.ScanRun(XgTagInfos, PerRequestDelay);
        });

      
        return true;
    }

    public override bool Connect()
    {
        if (_IsConnected)
            return true;

        try
        {
            var ping = new Ping();

            if (ping.Send(_Ip.Split(':')[0]).Status != IPStatus.Success)
            {
                throw new HWExceptionChannel($"해당 {_Ip} 와 연결을 확인하세요");
            }
            else
            {
                 _cpu = new XGTConnection(_Ip);
                _IsConnected = _cpu.Connect();
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
