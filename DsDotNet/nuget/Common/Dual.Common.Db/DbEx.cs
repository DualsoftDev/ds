using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Dapper;

using System.Data.SQLite;

namespace Dual.Common.Db
{
    public static class DbEx
    {
        public static bool IsSqlite(this IDbConnection connection) =>
            connection.GetType().FullName.IsOneOf("Microsoft.Data.Sqlite.SqliteConnection", "System.Data.SQLite.SQLiteConnection");
        public static bool IsSqlServer(this IDbConnection connection) =>
            connection.GetType().FullName == "System.Data.SqlClient.SqlConnection";
        public static async Task<string[]> GetTableNamesAsync(this IDbConnection connection)
        {
            string sql;
            if (connection.IsSqlite())
                sql = $"SELECT name FROM sqlite_master WHERE type = 'table'";
            else if (connection.IsSqlServer())
                sql = $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES";
            else
                throw new Exception($"Unsupported database type: {connection.GetType().Name}");

            var result = await connection.QueryAsync<string>(sql);
            return result.ToArray();
        }

        public static async Task<bool> IsTableExistsAsync(this IDbConnection connection, string tableName)
        {
            var tableNames = await GetTableNamesAsync(connection);
            return tableNames.Contains(tableName);
        }

        /// <summary>
        /// insert SQL 을 수행하고, 해당 inserted 행의 id 를 반환
        /// </summary>
        public static async Task<int> InsertAndQueryLastRowIdAsync(this IDbConnection connection, IDbTransaction transaction
            , string sql, object param)
        {
            Debug.Assert(sql.ToUpper().Contains("INSERT INTO"));
            string fnLastInsertedRowId;
            if (connection.IsSqlite())
                fnLastInsertedRowId = "LAST_INSERT_ROWID()";
            else if (connection.IsSqlServer())
                fnLastInsertedRowId = "SCOPE_IDENTITY()";
            else
                throw new Exception($"Unsupported database type: {connection.GetType().Name}");

            var newsql = sql + $"; SELECT CAST({fnLastInsertedRowId} as int);";
            if (transaction == null)
                return await connection.QuerySingleAsync<int>(newsql, param);
            else
                return await connection.QuerySingleAsync<int>(newsql, param, transaction);
        }

        public static async Task ExecuteSilentlyAsync(this IDbConnection connection, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            var ignoredResult = await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
        }


        public static async Task TruncateTableAsync(this DbConnection conn, DbTransaction transaction, string tableName)
        {
            if (conn is SQLiteConnection)
            {
                await conn.ExecuteAsync($"DELETE FROM [{tableName}];", transaction);
                await conn.ExecuteAsync($"UPDATE SQLITE_SEQUENCE SET seq = 0 WHERE name = '{tableName}';", transaction);
            }
            else
            {
                try
                {
                    await conn.ExecuteAsync($"TRUNCATE TABLE [{tableName}];", transaction);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Exception on TruncateTableAsync:\r\n{ex.Message}");
                    await conn.ExecuteAsync($"DELETE FROM [{tableName}];", transaction);
                    await conn.ExecuteAsync($"DBCC CHECKIDENT ('[{tableName}]', RESEED, 0);", transaction);
                }

            }
        }


        public static async Task TruncateTableAsync(this DbConnection conn, string tableName)
        {
            if (conn is SQLiteConnection)
            {
                await conn.ExecuteAsync($"DELETE FROM [{tableName}];");
                await conn.ExecuteAsync($"UPDATE SQLITE_SEQUENCE SET seq = 0 WHERE name = '{tableName}';");
            }
            else
            {
                try
                {
                    await conn.ExecuteAsync($"TRUNCATE TABLE [{tableName}];");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Exception on TruncateTableAsync:\r\n{ex.Message}");
                    await conn.ExecuteAsync($"DELETE FROM [{tableName}];");
                    await conn.ExecuteAsync($"DBCC CHECKIDENT ('[{tableName}]', RESEED, 0);");
                }
            }
        }


        /// <summary>
        /// Database 삭제.  db 내의 모든 table, view, index 를 삭제한다.
        /// </summary>
        public static void DropDatabase(this IDbConnection conn)
        {
            void DropDatabaseSQLite(IDbConnection connection)
            {
                void DropAll(IDbConnection dbConn, string objectType)
                {
                    var query = $"SELECT 'DROP {objectType} IF EXISTS \"' || name || '\";' AS Cmd FROM sqlite_master WHERE type = '{objectType}' AND name NOT LIKE 'sqlite_%';";
                    var commands = dbConn.Query<string>(query).ToArray();
                    foreach (var command in commands)
                    {
                        dbConn.Execute(command);
                    }
                }

                // Disable foreign key constraints
                connection.Execute("PRAGMA foreign_keys = OFF;");

                // Begin transaction
                connection.Execute("BEGIN TRANSACTION;");

                // Drop all tables
                DropAll(connection, "table");

                // Drop all indexes
                DropAll(connection, "index");

                // Drop all views
                DropAll(connection, "view");

                // Commit transaction
                connection.Execute("COMMIT;");

                // Enable foreign key constraints
                connection.Execute("PRAGMA foreign_keys = ON;");
            }

            void DropDatabaseMSSQL(IDbConnection connection)
            {

                // Disable foreign key constraints
                connection.Execute("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT all'");

                // Drop all tables
                var dropTables = "EXEC sp_MSforeachtable 'DROP TABLE ?'";
                connection.Execute(dropTables);

                // Drop all views
                var dropViews = @"
                DECLARE @name NVARCHAR(128);
                DECLARE cur CURSOR FOR SELECT name FROM sys.views;
                OPEN cur;
                FETCH NEXT FROM cur INTO @name;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC('DROP VIEW ' + @name);
                    FETCH NEXT FROM cur INTO @name;
                END;
                CLOSE cur;
                DEALLOCATE cur";
                connection.Execute(dropViews);

                // Drop all procedures
                var dropProcedures = @"
                DECLARE @name NVARCHAR(128);
                DECLARE cur CURSOR FOR SELECT name FROM sys.procedures;
                OPEN cur;
                FETCH NEXT FROM cur INTO @name;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC('DROP PROCEDURE ' + @name);
                    FETCH NEXT FROM cur INTO @name;
                END;
                CLOSE cur;
                DEALLOCATE cur";
                connection.Execute(dropProcedures);

                // Enable foreign key constraints
                connection.Execute("EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT all'");
            }

            var dbType = conn.GetType().Name.ToLower();
            switch (dbType)
            {
                case "sqliteconnection":
                    DropDatabaseSQLite(conn);
                    break;
                case "sqlconnection":
                    DropDatabaseMSSQL(conn);
                    break;
                default:
                    throw new NotSupportedException("Database type not supported.");
            }
        }
    }
}
