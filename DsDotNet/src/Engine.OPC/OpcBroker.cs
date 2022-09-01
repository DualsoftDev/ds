using Engine.Core;

using log4net;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;

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
    Dictionary<string, OpcTag> _tagDic = new Dictionary<string, OpcTag>();
    public OpcTag GetTag(string name) => _tagDic.ContainsKey(name) ? _tagDic[name] : null;
    public IEnumerable<string> Tags => _tagDic.Values.Select(ot => ot.Name);

    CompositeDisposable _disposables = new();

    public OpcBroker()
    {
        var subs = Global.TagChangeToOpcServerSubject.Subscribe(otc =>
        {
            if (_tagDic.ContainsKey(otc.TagName))
            {
                var otag = _tagDic[otc.TagName];
                if (otag.Value == otc.Value)
                    Global.Logger.Debug($"\t\tSkipping duplicated Publishing tag[{otc.TagName}] change = {otc.Value}");
                else
                {
                    Global.Logger.Debug($"\t\tPublishing tag[{otc.TagName}] change = {otc.Value}");
                    Write(otc.TagName, otc.Value);
                }
            }
        });
        _disposables.Add(subs);
    }

    public void AddTags(IEnumerable<Tag> tags)
    {
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

                Global.Logger.Debug($"OPC: tag[{opcTag.Name}] duplicated.");
            }
            else
                _tagDic.Add(opcTag.Name, opcTag);
    }


    public void Write(string tagName, bool value)
    {
        if (tagName == "ResetPlan_L_F_Main")
            Global.NoOp();

        // unit test 가 아니라면 무조건 실행되어야 할 부분.  unit test 에서만 생략 가능
        void doWrite()
        {
            var bit = _tagDic[tagName];
            if (bit.Value != value)
            {
                bit.SetValue(value);

                Global.TagChangeFromOpcServerSubject.OnNext(new OpcTagChange(tagName, value));
            }
        }

        if (! Global.IsInUnitTest || _tagDic.ContainsKey(tagName))
            doWrite();
    }

    public IEnumerable<(string, bool)> ReadTags(IEnumerable<string> tags)
    {
        foreach(var tag in tags)
        {
            if (_tagDic.ContainsKey(tag))
                yield return (tag, _tagDic[tag].Value);
        }
    }
}

public static class OpcBrokerExtension
{
    static ILog Logger => Global.Logger;
    public static void Print(this OpcBroker opc)
    {
        var tags = String.Join("\r\n\t", opc.Tags);
        Logger.Debug($"== OPC Tags:\r\n\t{tags}");
    }
}
