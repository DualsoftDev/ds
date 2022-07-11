using Dsu.Common.Utilities.ExtensionMethods;

using Engine.Core;

using log4net;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Engine.OPC
{
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
        internal List<CpuBase> _cpus = new List<CpuBase>();
        // }

        public void AddTags(IEnumerable<Tag> tags)
        {
            foreach(var opcTag in tags.Select(t => new OpcTag(t)))
                if (! _tagDic.ContainsKey(opcTag.Name))
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
    }

    public static class OpcBrokerExtension
    {
        static ILog Logger => Global.Logger;
        public static void Print(this OpcBroker opc)
        {
            Logger.Debug("== OPC");
            var tags = String.Join("\r\n\t", opc.Tags);
            Logger.Debug("\r\n\t" + tags);
        }
    }
}
