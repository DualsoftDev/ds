using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dual.PLC.Common
{
    public interface IObservableEvent
    {
    }

    public class TagEvent : IObservableEvent
    {
        public TagBase Tag { get; private set; }
        public TagEvent(TagBase tag) { Tag = tag; }

    }
    public class TagAddEvent : TagEvent
    {
        public TagAddEvent(TagBase tag) : base(tag) {}
    }

    public class TagsAddEvent : IObservableEvent
    {
        public List<TagBase> Tags;

        public TagsAddEvent(IEnumerable<TagBase> tags)
        {
            Tags = tags.ToList();
        }
    }

    public class TagValueChangedEvent : TagEvent
    {
        public TagValueChangedEvent(TagBase tag) : base(tag) { }
    }
}
