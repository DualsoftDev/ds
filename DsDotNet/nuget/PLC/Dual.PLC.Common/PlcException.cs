using System;
using System.Collections.Generic;
using System.Linq;

namespace Dual.PLC.Common
{
    public class PlcException : Exception
    {
        public PlcException() : base() {}
        public PlcException(string message) : base(message) {}
    }

    public class PlcExceptionRead : PlcException
	{
		public PlcExceptionRead() : base() { }
		public PlcExceptionRead(string message) : base(message) { }
	}

    public class PlcExceptionTag : PlcException
    {
        public TagBase Tag { get; set; }
        public PlcExceptionTag() : base() { }
        public PlcExceptionTag(string message, TagBase tag=null) : base(message) { Tag = tag; }
    }


    public class PlcExceptionChannel : PlcException
    {
        public Exception OriginalException { get; internal set; }
        public List<TagBase> Tags { get; internal set; }

        public PlcExceptionChannel(string message, Exception innerException=null, IEnumerable<TagBase> tags=null)
        {
            OriginalException = innerException;
            Tags = tags?.ToList();
        }

    }
}
