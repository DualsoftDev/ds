using System;
using System.Threading;
using Dsu.Common.Utilities.ExtensionMethods;

namespace Dsu.Common.Utilities
{
    /// <summary>
    /// Extension methods for creating read/write locker.
    /// <br/> - Allows null rwLock : null 인 경우, Disposable.EmptyInstance
    /// </summary>
    public static class EmReadWriteLockerSlim
    {
        public static IDisposable CreateReadLocker(this ReaderWriterLockSlim rwLock, int millisecondsTimeout = Timeout.Infinite)
        {
            if (rwLock == null)
                return Disposable.EmptyInstance;

            return new ReaderLockerSlim(rwLock, millisecondsTimeout);
        }

        public static IDisposable CreateWriteLocker(this ReaderWriterLockSlim rwLock, int millisecondsTimeout = Timeout.Infinite)
        {
            if (rwLock == null)
                return Disposable.EmptyInstance;

            return new WriterLockerSlim(rwLock, millisecondsTimeout);
        }

    }


    /// <summary>
    /// ReaderWriterLockSlim 을 이용한 ReaderLock acquire/release 클래스
    /// </summary>
    public class ReaderLockerSlim : IDisposable
    {
        private ReaderWriterLockSlim _rwLock;

        public ReaderLockerSlim(ReaderWriterLockSlim rwLock, TimeSpan timeout)
        {
            _rwLock = rwLock;
            if ( ! _rwLock.TryEnterUpgradeableReadLock(timeout))
                throw new Exception("Failed to acquire read lock");
        }
        public ReaderLockerSlim(ReaderWriterLockSlim rwLock, int millisecondsTimeout = Timeout.Infinite)
        {
            _rwLock = rwLock;
            if (!_rwLock.TryEnterUpgradeableReadLock(millisecondsTimeout))
                throw new Exception("Failed to acquire read lock");
        }

        public void Dispose()
        {
            _rwLock.ExitUpgradeableReadLock();
        }
    }


    /// <summary>
    /// ReaderWriterLockSlim 을 이용한 WriterLock acquire/release 클래스
    /// </summary>
    public class WriterLockerSlim : IDisposable
    {
        private ReaderWriterLockSlim _rwLock;

        public WriterLockerSlim(ReaderWriterLockSlim rwLock, TimeSpan timeout)
        {
            _rwLock = rwLock;

            if ( ! _rwLock.TryEnterWriteLock(timeout) )
                throw new Exception("Failed to acquire write lock");
        }

        public WriterLockerSlim(ReaderWriterLockSlim rwLock, int millisecondsTimeout = Timeout.Infinite)
        {
            _rwLock = rwLock;
        
            if ( ! _rwLock.TryEnterWriteLock(millisecondsTimeout) )
                throw new Exception("Failed to acquire write lock");
        }

        public void Dispose()
        {
            _rwLock.ExitWriteLock();
        }
    }




    /// <summary>
    /// ReaderWriterLock 을 이용한 ReaderLock acquire/release 클래스
    /// </summary>
    public class ReaderLocker : IDisposable
    {
        private ReaderWriterLock _rwLock;

        public ReaderLocker(ReaderWriterLock rwLock, TimeSpan timeout)
        {
            _rwLock = rwLock;
            _rwLock.AcquireReaderLock(timeout);
        }
        public ReaderLocker(ReaderWriterLock rwLock, int millisecondsTimeout = Timeout.Infinite)
        {
            _rwLock = rwLock;
            _rwLock.AcquireReaderLock(millisecondsTimeout);
        }

        public void Dispose()
        {
            _rwLock.ReleaseReaderLock();
        }
    }


    /// <summary>
    /// ReaderWriterLock 을 이용한 WriterLock acquire/release 클래스
    /// </summary>
    public class WriterLocker : IDisposable
    {
        private ReaderWriterLock _rwLock;

        public WriterLocker(ReaderWriterLock rwLock, TimeSpan timeout)
        {
            _rwLock = rwLock;
            _rwLock.AcquireWriterLock(timeout);
        }
        public WriterLocker(ReaderWriterLock rwLock, int millisecondsTimeout = Timeout.Infinite)
        {
            _rwLock = rwLock;
            _rwLock.AcquireWriterLock(millisecondsTimeout);
        }

        public void Dispose()
        {
            _rwLock.ReleaseWriterLock();
        }
    }


}
