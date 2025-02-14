using System.Data.SqlClient;

namespace Dual.Common.Db;

public static class SqlServerExtension
{
    public static bool BackupSqlServerDatabase(string connectionString, string database, string backupPath)
    {
        var sql = $"BACKUP DATABASE [{database}] TO DISK = '{backupPath}' WITH FORMAT;";
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
        return true;
    }

    public static bool RestoreSqlServerDatabase(string connectionString, string database, string backupPath)
    {
        var sqlSimplestRestoreCommand = $"RESTORE DATABASE [{database}] FROM DISK = '{backupPath}'";
        string sql = $@"
USE master;
ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [{database}] FROM DISK = '{backupPath}' WITH REPLACE;
ALTER DATABASE [{database}] SET MULTI_USER;";
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
        return true;
    }

    public static bool DropDatabase(string connectionString, string database)
    {
        string sql = $@"
USE master;
ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [{database}];"
;
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
        return true;
    }
}
