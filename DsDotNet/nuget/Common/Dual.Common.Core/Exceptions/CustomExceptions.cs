using System;

namespace Dual.Common.Core
{
    /// <summary>
    /// Subclass should have re-implemented the method by overriding, but it did not.
    /// </summary>
    public class NotReimplementedException : NotImplementedException
    {
        public NotReimplementedException() { }
        public NotReimplementedException(string message) : base(message) { }
    }

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
