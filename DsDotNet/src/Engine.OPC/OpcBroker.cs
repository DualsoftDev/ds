using Dsu.Common.Utilities.ExtensionMethods;

using Engine.Core;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.OPC
{
    class OpcTag : Bit
    {
        Tag _originalTag;
        public OpcTag(Tag tag)
            : base(tag.Name, tag.Value)
        {
            _originalTag = tag;
        }
    }
    public class OpcBroker
    {
        HashSet<OpcTag> Tags = new HashSet<OpcTag>();
        public void AddTags(IEnumerable<Tag> tags)
        {
            foreach(var opcTag in tags.Select(t => new OpcTag(t)))
                Tags.Add(opcTag);
        }
    }
}
