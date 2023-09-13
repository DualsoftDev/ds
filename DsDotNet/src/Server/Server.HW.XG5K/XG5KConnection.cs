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

namespace Server.HW.XG5K;

public class XG5KConnection : ConnectionBase
{
    private int _InCnt;
    private int _OutCnt;
    private string _Ip;
    internal IEnumerable<XG5KTag> XG5KTags => Tags.Values.OfType<XG5KTag>();

    private XG5KConnectionParameters _connectionParameters;
    private DsXgConnection _cpu = new();

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
  
    public void Stop() => _cpu?.Stop();
    public bool Start()
    {
        if (!_IsConnected)
            return false;


        Task.Run(() =>
        {
            var tags = XG5KTags.Select(s => s.Address);
            var xgTags = MonitorUtil.creatTags(tags);
            xgTags.Iter(t =>
            {
                Tags.Values.Where(w => w.Address == t.Tag)
                           .OfType<XG5KTag>()
                           .Iter(xg5kTag => xg5kTag.XgPLCTag = t);
            });

            var addressDic = Tags.Values.ToDictionary(s => s.Address, d => d);

            XGTagModule.PLCTagSubject.Subscribe(x =>
            {
                var tag = addressDic[x.Tag];
                tag.Value = x.Value;
                Trace.WriteLine($"{x.Tag} => {x.Value}");
            });

            _cpu.Scan(xgTags, PerRequestDelay);
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
                _IsConnected = _cpu.Connect(_Ip);
                
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
