using Engine.Core;

using log4net;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.OPC;

public class OpcTag : Bit
{
    internal Tag OriginalTag;
    public OpcTag(Tag tag)
        : base(tag.Name, tag.Value)
    {
        OriginalTag = tag;
    }
}
public class OpcBroker
{
    Dictionary<string, OpcTag> _tagDic = new Dictionary<string, OpcTag>();
    public IEnumerable<string> Tags => _tagDic.Values.Select(ot => ot.Name);

    // { Debug only, or temporary implementations
    internal IEnumerable<OpcTag> _opcTags => _tagDic.Values;
    internal List<Cpu> _cpus = new List<Cpu>();
    // }

    public void AddTags(IEnumerable<Tag> tags)
    {
        foreach (var opcTag in tags.Select(t => new OpcTag(t)))
            if (_tagDic.ContainsKey(opcTag.Name))
                Global.Logger.Debug($"OPC: tag[{opcTag.Name}] duplicated.");
            else
                _tagDic.Add(opcTag.Name, opcTag);
    }


    public void Write(string tagName, bool value)
    {
        var bit = _tagDic[tagName];
        if (bit.Value != value)
        {
            bit.Value = value;

            foreach (var cpu in _cpus)
                cpu.OnOpcTagChanged(tagName, value);
        }
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
