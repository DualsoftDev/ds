using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.HW.Common
{
    public interface IObservableEvent
    {
    }

    public class TagEvent : IObservableEvent
    {
        public TagHW Tag { get; private set; }
        public TagEvent(TagHW tag) { Tag = tag; }

    }
    public class TagAddEvent : TagEvent
    {
        public TagAddEvent(TagHW tag) : base(tag) {}
    }

    public class TagsAddEvent : IObservableEvent
    {
        public List<TagHW> Tags;

        public TagsAddEvent(IEnumerable<TagHW> tags)
        {
            Tags = tags.ToList();
        }
    }

    public class TagValueChangedEvent : TagEvent
    {
        public TagValueChangedEvent(TagHW tag) : base(tag) { }
    }
}
