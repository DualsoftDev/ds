using System;

namespace Dsu.Common.Utilities
{
	/// <summary>
	/// Subclass will not re-implemented.  So you should not call this subclass method.
	/// </summary>
	public class WillNotBeReimplementedException : NotImplementedException
	{
		public WillNotBeReimplementedException() { }
		public WillNotBeReimplementedException(string message) : base(message) { }
	}

	public class UnexpectedCaseOccurredException : InvalidOperationException
    {
        public UnexpectedCaseOccurredException() { }
        public UnexpectedCaseOccurredException(string message) : base(message) { }
    }

    public class SampleException : Exception
    {
        public SampleException() { }
        public SampleException(string message) : base(message) { }        
    }
}
