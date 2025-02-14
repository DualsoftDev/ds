using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dual.Common.Db
{
    /// <summary>
    /// DB connection 에 대해서
    /// transaction 이 null 로 주어지면 transaction scope 을 생성하고, scope 종료시 commit 하는 scope를 생성해서 반환한다.
    /// transaction 이 non-null 로 주어지면, 아무런 작업을 수행하지 않는다.  (상위에서 transaction 을 관리하는 것으로 간주)
    /// </summary>
    public class DbTransactionScope : IDisposable
    {
        IDbTransaction _transaction;

        public DbTransactionScope(IDbConnection conn, IDbTransaction noTransaction)
        {
            if (conn.IsSqlServer())
                throw new Exception("DbTransactionScope: SQL Server not supported");

            _transaction = noTransaction ?? conn.BeginTransaction();
        }

        public void Dispose()
        {
            if (_transaction != null)
            {
                _transaction.Commit();
                _transaction.Dispose();
                _transaction = null;
            }
        }
    }

    public static class DbWrapperEx
    {
        public static IDisposable TransactionScope(this IDbConnection conn, IDbTransaction noTransaction)
        {
            return new DbTransactionScope(conn, noTransaction);
        }
    }
}
