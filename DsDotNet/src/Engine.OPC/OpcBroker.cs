using Engine.Core;
using Dsu.PLC;
using Dsu.PLC.LS;
using Dsu.PLC.Common;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
using Microsoft.FSharp.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using static Engine.Core.GlobalShortCuts;

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
    public OpcTag GetTag(string name) => _tagDic.ContainsKey(name) ? _tagDic[name] : null;
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

    internal LsConnection Conn { get; }
    public OpcBroker()
    {
        if (Core.Global.IsControlMode)
        {
            LogDebug("Starting PLC connection.");
            var param = new LsConnectionParameters(
                "192.168.0.100", new FSharpOption<ushort>(2004),
                TransportProtocol.Tcp, 3000.0
            );

            Conn = new LsConnection(param);
        }

        var subs = Core.Global.TagChangeToOpcServerSubject.Subscribe(otc =>
        {
            var n = otc.TagName;
            if (_tagDic.ContainsKey(n))
                Write(n, otc.Value);
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
            {
                var existing = _tagDic[opcTag.Name];
                Debug.Assert(existing.Name == opcTag.Name);
                switch (existing.OriginalTag, opcTag.OriginalTag)
                {
                    case (TagA exs, TagA neo):
                        Debug.Assert(exs.Address == neo.Address);
                        break;
                    case (TagA tagA, Tag neo): break;
                    case (Tag exs, TagA neo):
                        _tagDic[opcTag.Name] = opcTag;
                        break;
                }

                LogDebug($"OPC: tag[{opcTag.Name}] duplicated.");
            }
            else
                _tagDic.Add(opcTag.Name, opcTag);
    }

    public void AddLsBits(string tagName, string memAddr)
    {
        if (Conn == null) return;
        Console.WriteLine(tagName  + " : " + memAddr);
        LsBits.Add((LsTag)Conn.CreateTag(memAddr));
        IdxLsBits[tagName] = IdxLsBits.Count;
        LsBits.Last().Value = false;
    }

    public void UpdateLsBits(string tagName, bool value)
    {
        var idx = IdxLsBits[tagName];
        var tag = LsBits[idx];
        tag.Value = value;
        Conn.WriteRandomTags(new[] {tag} );
    }

    public void Write(string tagName, bool value)
    {
        if (tagName == "ResetPlan_L_F_Main")
            Core.Global.NoOp();

        // unit test 가 아니라면 무조건 실행되어야 할 부분.  unit test 에서만 생략 가능
        void doWrite()
        {
            var bit = _tagDic[tagName];
            if (bit.Value != value)
            {
                LogDebug($"\t\tPublishing tag[{tagName}] change = {value}");
                bit.SetValue(value);
                if (tagToAddr.ContainsKey(tagName) && Conn != null)
                {
                    Console.WriteLine("Write - " + tagName + " : " + value);
                    UpdateLsBits(tagName, value);
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
            Conn.Subject
                .OfType<TagValueChangedEvent>()
                .Subscribe(evt => {
                    var lsTag = (LsTag)evt.Tag;
                    var addr = lsTag.Name;
                    var tagName = addrToTag[addr];
                    var value = (bool)lsTag.Value;
                    LogDebug($"Read from channel: {tagName} = {value}");
                    if (addr[1] == 'I')
                        Read(tagName, value);
                });
            Console.WriteLine("Ready!");
            await Conn.StartDataExchangeLoopAsync();
        }
    }
}

public static class OpcBrokerExtension
{
    public static void Print(this OpcBroker opc)
    {
        var tags = String.Join("\r\n\t", opc.Tags);
        LogDebug($"== OPC Tags:\r\n\t{tags}");
    }
}