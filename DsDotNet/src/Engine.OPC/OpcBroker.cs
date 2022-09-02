using Engine.Core;
using Dsu.PLC;
using Dsu.PLC.LS;
using Dsu.PLC.Common;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
using Microsoft.FSharp.Core;
using log4net;

using System;
using System.Net;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Engine.OPC;

public class OpcTag : Bit, IBitReadWritable
{
    internal Tag OriginalTag;
    public OpcTag(Tag tag)
        : base(tag.Name, tag.Value)
    {
        OriginalTag = tag;
    }

    public void SetValue(bool newValue) => _value = newValue;
}

public class OpcBroker
{
    Dictionary<string, OpcTag> _tagDic = new();
    public IEnumerable<string> Tags => _tagDic.Values.Select(ot => ot.Name);

    // { Debug only, or temporary implementations
    internal IEnumerable<OpcTag> _opcTags => _tagDic.Values;
    // }

    Dictionary<string, string> tagToAddr = new();
    Dictionary<string, string> addrToTag = new();

    CompositeDisposable _disposables = new();

    // 192.168.0.100
    static public string plcAdress { get; set; }

    internal List<LsTag> LsBits = new();
    internal Dictionary<string, int> IdxLsBits = new();

    internal LsConnection Conn =
        new LsConnection(
            new LsConnectionParameters(
                "192.168.0.100", new FSharpOption<ushort>(2004),
                TransportProtocol.Tcp, 3000.0
            )
        );

    public OpcBroker()
    {
        var subs = Core.Global.TagChangeToOpcServerSubject.Subscribe(otc =>
        {
            if (_tagDic.ContainsKey(otc.TagName))
            {
                Core.Global.Logger.Debug($"\t\tPublishing tag[{otc.TagName}] change = {otc.Value}");
                Write(otc.TagName, otc.Value);
            }
        });
        _disposables.Add(subs);
    }

    public void AddTags(IEnumerable<Tag> tags)
    {
        // for LS PLC
        foreach (var t in tags)
            if (t.GetType() == typeof(TagA))
            {
                var at = (TagA)t;
                tagToAddr[at.Name] = at.Address;
                addrToTag[at.Address] = at.Name;
                AddLsBits(at.Name, at.Address);
            }

        foreach (var opcTag in tags.Select(t => new OpcTag(t)))
            if (_tagDic.ContainsKey(opcTag.Name))
                Core.Global.Logger.Debug($"OPC: tag[{opcTag.Name}] duplicated.");
            else
                _tagDic.Add(opcTag.Name, opcTag);
    }

    public void AddLsBits(string tagName, string memAddr)
    {
        Console.WriteLine(tagName  + " : " + memAddr);
        LsBits.Add((LsTag)Conn.CreateTag(memAddr));
        IdxLsBits[tagName] = IdxLsBits.Count;
        LsBits.Last().Value = false;
    }

    public void UpdateListBits(string tagName, bool value)
    {
        var idx = IdxLsBits[tagName];
        LsBits[idx].Value = value;
        Conn.WriteRandomTags(LsBits.ToArray());
    }

    public void Write(string tagName, bool value)
    {
        // unit test 가 아니라면 무조건 실행되어야 할 부분.  unit test 에서만 생략 가능
        void doWrite()
        {
            var bit = _tagDic[tagName];
            if (bit.Value != value)
            {
                bit.SetValue(value);
                if (tagToAddr.ContainsKey(tagName))
                {
                    Console.WriteLine("Write - " + tagName + " : " + value);
                    UpdateListBits(tagName, value);
                }
                Core.Global.TagChangeFromOpcServerSubject.OnNext(new OpcTagChange(tagName, value));
            }
        }

        if (!Core.Global.IsInUnitTest || _tagDic.ContainsKey(tagName))
            doWrite();
    }

    public void Read(string tagName, bool value)
    {
        // unit test 가 아니라면 무조건 실행되어야 할 부분.  unit test 에서만 생략 가능
        void doRead()
        {
            var bit = _tagDic[tagName];
            if (bit.Value != value)
            {
                LsBits[IdxLsBits[tagName] - 1].Value = false;
                Conn.WriteRandomTags(LsBits.ToArray());
                Console.WriteLine("Read - " + tagName + " : " + value);
                bit.SetValue(value);
                Core.Global.TagChangeFromOpcServerSubject.OnNext(new OpcTagChange(tagName, value));
            }
        }

        if (!Core.Global.IsInUnitTest || _tagDic.ContainsKey(tagName))
            doRead();
    }

    public IEnumerable<(string, bool)> ReadTags(IEnumerable<string> tags)
    {
        foreach (var tag in tags)
        {
            if (_tagDic.ContainsKey(tag))
                yield return (tag, _tagDic[tag].Value);
        }
    }

    public void StreamData()
    {   
        Core.Global.BitChangedSubject
            .Subscribe(bc =>
            {
                var n = bc.Bit.GetName();
                var val = bc.Bit.Value;
                //Console.WriteLine($"name : {n}, value : {val}");
            }
        );
    }

    public async Task CommunicationPLC()
    {
        Conn.PerRequestDelay = 20;
        if (Conn.Connect())
        {
            Conn.AddMonitoringTags(LsBits);
            LsBits[IdxLsBits["StartActual_A_F_Plus"]].Value = false;
            LsBits[IdxLsBits["StartActual_B_F_Plus"]].Value = false;
            LsBits[IdxLsBits["StartActual_C_F_Plus"]].Value = false;
            LsBits[IdxLsBits["StartActual_D_F_Plus"]].Value = false;
            LsBits[IdxLsBits["StartActual_A_F_Minus"]].Value = false;
            LsBits[IdxLsBits["StartActual_B_F_Minus"]].Value = false;
            LsBits[IdxLsBits["StartActual_C_F_Minus"]].Value = false;
            LsBits[IdxLsBits["StartActual_D_F_Minus"]].Value = false;
            Conn.WriteRandomTags(LsBits.ToArray());
            Conn.Subject
                .OfType<TagValueChangedEvent>()
                .Subscribe(evt =>
                    {
                        var tag = (LsTag)evt.Tag;
                        if (tag.Name[1] == 'I')
                            Read(addrToTag[tag.Name], (bool)tag.Value);
                    }
                );
            Console.WriteLine("Ready!");
            await Conn.StartDataExchangeLoopAsync();
        }
    }
}

public static class OpcBrokerExtension
{
    static ILog Logger => Core.Global.Logger;
    public static void Print(this OpcBroker opc)
    {
        var tags = String.Join("\r\n\t", opc.Tags);
        Logger.Debug($"== OPC Tags:\r\n\t{tags}");
    }
}