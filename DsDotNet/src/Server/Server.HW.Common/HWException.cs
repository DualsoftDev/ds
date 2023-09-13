using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;

namespace Server.HW.Common
{
    public interface IHwExceptionEvent    {    }

    public abstract class HWException : Exception, IHwExceptionEvent
    {
        public Subject<IHwExceptionEvent> SubjectHwExceptionEvent { get; } = new Subject<IHwExceptionEvent>();
        public HWException() : base() { SubjectHwExceptionEvent.OnNext(this); }
        public HWException(string message) : base(message) { }
    }

    public class HWExceptionRead : HWException
    {
        public HWExceptionRead() : base() { }
        public HWExceptionRead(string message) : base(message) { }
    }

    public class HWExceptionWrite: HWException
    {
        public HWExceptionWrite() : base() { }
        public HWExceptionWrite(string message) : base(message) { }
    }

    public class HWExceptionTag : HWException
    {
        public TagHW Tag { get; set; }
        public HWExceptionTag() : base() { }
        public HWExceptionTag(string message, TagHW tag = null) : base(message) { Tag = tag; }
    }


    public class HWExceptionChannel : HWException
    {
        public Exception OriginalException { get; internal set; }
        public List<TagHW> Tags { get; internal set; }

        public HWExceptionChannel(string message, Exception innerException = null, IEnumerable<TagHW> tags = null)
        {
            OriginalException = innerException;
            Tags = tags?.ToList();
        }

    }
}
