using System;
using System.Xml;

namespace Dual.Common.Core
{
    public class ScriptTaskException : ApplicationException
    {
        public ScriptTaskException() { }
        public ScriptTaskException(string message) : base(message) { }
    }

    internal class ExceptionSample
    {
        public void DoNothing()
        {
            throw new System.Exception();
            throw new System.ApplicationException();
            throw new System.SystemException();
            throw new System.NotImplementedException();

            throw new System.ArgumentException();
            throw new System.ArgumentNullException();
            throw new System.ArgumentOutOfRangeException();
            throw new System.FormatException();
            throw new System.RankException();
            throw new System.InvalidOperationException();
            throw new System.InvalidCastException();
            throw new System.NotSupportedException();
            throw new System.IndexOutOfRangeException();

            throw new System.InsufficientMemoryException();

            throw new System.FieldAccessException();
            throw new System.MemberAccessException();
            throw new System.MethodAccessException();

            throw new System.MissingFieldException();
            throw new System.MissingMemberException();
            throw new System.MissingMethodException();




/*
            throw new AccessViolationException();
            throw new AppDomainUnloadedException();
            throw new ApplicationException();
            throw new ArgumentException();
            throw new ArgumentNullException();
            throw new ArgumentOutOfRangeException();
            throw new ArithmeticException();
            throw new ArrayTypeMismatchException();
            throw new BadImageFormatException();
            throw new CannotUnloadAppDomainException();
            throw new ContextMarshalException();
            throw new DataMisalignedException();
            throw new DivideByZeroException();
            throw new DllNotFoundException();
            throw new DuplicateWaitObjectException();
            throw new EntryPointNotFoundException();
            throw new Exception();
            throw new ExecutionEngineException();
            throw new FieldAccessException();
            throw new FormatException();
            throw new IndexOutOfRangeException();
            throw new InsufficientMemoryException();
            throw new InvalidCastException();
            throw new InvalidOperationException();
            throw new InvalidProgramException();
            throw new MemberAccessException();
            throw new MethodAccessException();
            throw new MissingFieldException();
            throw new MissingMemberException();
            throw new MissingMethodException();
            throw new MulticastNotSupportedException();
            throw new NotFiniteNumberException();
            throw new NotImplementedException();
            throw new NotSupportedException();
            throw new NullReferenceException();
            throw new ObjectDisposedException();
            throw new OperationCanceledException();
            throw new OutOfMemoryException();
            throw new OverflowException();
            throw new RankException();
            throw new StackOverflowException();
            throw new SystemException();
            throw new TimeoutException();
            throw new TypeInitializationException();
            throw new TypeLoadException();
            throw new TypeUnloadedException();
            throw new UnauthorizedAccessException();
            throw new UriFormatException();
            throw new XmlException();
 */

        }
    }
}
