using Dsu.Common.Utilities.ExtensionMethods;

using Engine.Core;

using log4net;

using System;
using System.Collections.Generic;
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
        public IEnumerable<OpcTag> Tags => _tagDic.Values;
        public void AddTags(IEnumerable<Tag> tags)
        {
            foreach(var opcTag in tags.Select(t => new OpcTag(t)))
                if (! _tagDic.ContainsKey(opcTag.Name))
                    _tagDic.Add(opcTag.Name, opcTag);
        }
    }

    public static class OpcBrokerHelper
    {
        static ILog Logger => Global.Logger;
        public static void Print(this OpcBroker opc)
        {
            Logger.Debug("== OPC");
            var tags = String.Join("\r\n\t", opc.Tags.Select(ot => ot.Name));
            Logger.Debug("\r\n\t" + tags);
        }
    }
}
