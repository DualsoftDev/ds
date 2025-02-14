using Dapper;

using Dual.Common.Core;
using Dual.Common.Db.Models.Attributes;
using System.Data.SQLite;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dual.Common.Db.Repositories.Base
{
    public abstract class BaseRepository
    {
        string _databaseType = "sqlserver";
        private readonly string _connectionString;
        protected BaseRepository(string databaseType, string sqlConnectionString)
        {
            if (! _databaseType.IsOneOf("sqlserver", "sqlite"))
                throw new Exception($"Unknown database type: {_databaseType}");

            _connectionString = sqlConnectionString;
            _databaseType = databaseType;
        }

        public bool IsMsSql => _databaseType == "sqlserver";

        public DbConnection CreateConnection()
        {
            return _databaseType switch
            {
                "sqlserver" => (new SqlConnection(_connectionString)).Tee(c => c.Open()),
                "sqlite" => (new SQLiteConnection(_connectionString)).Tee(c => c.Open()),
                _ => throw new Exception($"Unknown database type: {_databaseType}")
            };
        }

        public (DbConnection, DbTransaction) CreateConnectionWithTransaction()
        {
            var connection = CreateConnection();
            var transaction = connection.BeginTransaction();
            return (connection, transaction);
        }



        public async Task<IEnumerable<TEntity>> GetAsync<TEntity>(string tableName)
        {
            using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<TEntity>($"SELECT * FROM [{tableName}];").ConfigureAwait(false);
        }
    }
    /// <summary>
    /// Generic asynchronous base repository using Dapper
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class BaseRepositoryT<TEntity> : BaseRepository, IBaseRepository<TEntity> where TEntity : class
    {
        private readonly string _tableName;

        protected BaseRepositoryT(string databaseType, string sqlConnectionString, string tableName)
            : base(databaseType, sqlConnectionString)
        {
            _tableName = tableName;
        }

        /// <summary>
        /// Delete entity with id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> DeleteAsync(int id)
        {
            using DbConnection connection = CreateConnection();
            return await connection.ExecuteAsync($"DELETE FROM {_tableName} WHERE Id=@Id", new { Id = id }).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all entities
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<TEntity>> GetAsync()
        {
            using DbConnection connection = CreateConnection();
            return await connection.QueryAsync<TEntity>($"SELECT * FROM [{_tableName}];").ConfigureAwait(false);
        }

        /// <summary>
        /// Get entity by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<TEntity> GetAsync(int id)
        {
            using DbConnection connection = CreateConnection();
            var result = await connection.QuerySingleOrDefaultAsync<TEntity>($"SELECT * FROM [{_tableName}] WHERE Id=@Id", new { Id = id }).ConfigureAwait(false);
            if (result == null)
            {
                throw new KeyNotFoundException($"{_tableName} with Id [{id}] not found");
            }
            return result;
        }

        /// <summary>
        /// Insert new entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<int> InsertAsync(TEntity entity)
        {
            var insertQuery = CreateInsertQuery();
            using DbConnection connection = CreateConnection();
            return await connection.ExecuteAsync(insertQuery, entity).ConfigureAwait(false);
        }

        /// <summary>
        /// Update existing entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(TEntity entity)
        {
            var updateQuery = CreateUpdateQuery();
            using DbConnection connection = CreateConnection();
            return await connection.ExecuteAsync(updateQuery, entity).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets entity properties names as string list, except those with [Ignore] attribute
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private static List<string> GetPropertiesNames(IEnumerable<PropertyInfo> properties)
        {
            var result = new List<string>();
            foreach (var prop in properties)
            {
                var attributes = prop.GetCustomAttributes(typeof(Ignore), false);
                if (attributes.Length >= 1)
                {
                    continue;
                }
                result.Add(prop.Name);
            }
            return result;
        }

        /// <summary>
        /// Creates insert TSQL query
        /// </summary>
        /// <returns></returns>
        private string CreateInsertQuery()
        {
            var result = new StringBuilder($"INSERT INTO {_tableName} (");
            var entityProperties = typeof(TEntity).GetProperties();
            var propertiesNames = GetPropertiesNames(entityProperties);
            propertiesNames.ForEach(prop => result.Append($"[{prop}],"));
            result.Remove(result.Length - 1, 1).Append(") VALUES (");
            propertiesNames.ForEach(prop =>
            {
                result.Append($"@{prop},");
            });
            result.Remove(result.Length - 1, 1).Append(")");
            return result.ToString();
        }

        /// <summary>
        /// Creates update TSQL query
        /// </summary>
        /// <returns></returns>
        private string CreateUpdateQuery()
        {
            var result = new StringBuilder($"UPDATE {_tableName} SET ");
            var entityProperties = typeof(TEntity).GetProperties();
            var propertiesNames = GetPropertiesNames(entityProperties);
            propertiesNames.ForEach(property =>
            {
                if (!property.Equals("Id", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Append($"{property}=@{property},");
                }
            });
            result.Remove(result.Length - 1, 1);
            result.Append(" WHERE Id=@Id");
            return result.ToString();
        }
    }
}