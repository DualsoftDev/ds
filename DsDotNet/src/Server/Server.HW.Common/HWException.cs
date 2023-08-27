using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.HW.Common
{
    public class HWException : Exception
    {
        public HWException() : base() {}
        public HWException(string message) : base(message) {}
    }

    public class HWExceptionRead : HWException
	{
		public HWExceptionRead() : base() { }
		public HWExceptionRead(string message) : base(message) { }
	}

    public class HWExceptionTag : HWException
    {
        public TagHW Tag { get; set; }
        public HWExceptionTag() : base() { }
        public HWExceptionTag(string message, TagHW tag=null) : base(message) { Tag = tag; }
    }


    public class HWExceptionChannel : HWException
    {
        public Exception OriginalException { get; internal set; }
        public List<TagHW> Tags { get; internal set; }

        public HWExceptionChannel(string message, Exception innerException=null, IEnumerable<TagHW> tags=null)
        {
            OriginalException = innerException;
            Tags = tags?.ToList();
        }

    }
}
