using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据库工厂。
    /// </summary>
    public class DatabaseFactory
    {
        private static readonly Type DbConnectionType = typeof(System.Data.Common.DbConnection);

        /// <summary>
        /// 创建库。
        /// </summary>
        /// <param name="connectionConfig"></param>
        /// <returns></returns>
        public static IDatabase Create(IReadOnlyConnectionConfig connectionConfig)
        {
            if (connectionConfig is null)
            {
                throw new ArgumentNullException(nameof(connectionConfig));
            }

            var adapter = DbConnectionManager.Get(connectionConfig.ProviderName);

            if (DbConnectionType.IsAssignableFrom(adapter.DbConnectionType))
            {

            }

            return new Database(connectionConfig, adapter);
        }

        private class Transaction : IDbTransaction
        {
            private readonly Database database;

            public Transaction(Database database, IDbTransaction transaction)
            {
                this.database = database;
                DbTransaction = transaction;
            }

            public IDbConnection Connection => database;

            public IsolationLevel IsolationLevel => DbTransaction.IsolationLevel;

            public IDbTransaction DbTransaction { get; }

            public void Commit()
            {
                database.ResetTransaction();
                DbTransaction.Commit();
            }

            public void Dispose()
            {
                database.ResetTransaction();
                DbTransaction.Dispose();
            }

            public void Rollback()
            {
                database.ResetTransaction();
                DbTransaction.Rollback();
            }
        }

        /// <summary>
        /// 数据库。
        /// </summary>
        private class Database : IDatabase
        {
            private Transaction transaction;
            private IDbConnection connection;
            private readonly IDbConnectionLtsAdapter adapter;
            private readonly IReadOnlyConnectionConfig connectionConfig;

            /// <summary>
            /// 构造函数。
            /// </summary>
            public Database(IReadOnlyConnectionConfig connectionConfig, IDbConnectionLtsAdapter adapter)
            {
                this.connectionConfig = connectionConfig ?? throw new ArgumentNullException(nameof(connectionConfig));
                this.adapter = adapter;
            }

            /// <summary> 连接名称。 </summary>
            public string Name => connectionConfig.Name;

            /// <summary> 数据库驱动名称。 </summary>
            public string ProviderName => connectionConfig.ProviderName;

            /// <summary>
            /// SQL 矫正。
            /// </summary>
            public ISQLCorrectSettings Settings => adapter.Settings;

            public string ConnectionString { get => Connection.ConnectionString; set => Connection.ConnectionString = value; }

            public int ConnectionTimeout => Connection.ConnectionTimeout;

            string IDbConnection.Database => Connection.Database;

            public ConnectionState State => Connection.State;

            private IDbConnection Connection => connection ?? (connection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true));

            public void ResetTransaction() => transaction = null;

            public T Read<T>(Expression expression)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<T> Query<T>(Expression expression)
            {
                throw new NotImplementedException();
            }

            public Task<T> ReadAsync<T>(Expression expression, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public IAsyncEnumerable<T> QueryAsync<T>(Expression expression)
            {
                throw new NotImplementedException();
            }

            public int Execute(Expression expression)
            {
                throw new NotImplementedException();
            }

            public Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public IDbTransaction BeginTransaction() => BeginTransaction(IsolationLevel.ReadCommitted);

            public IDbTransaction BeginTransaction(IsolationLevel il)
            {
                if (transaction is null)
                {
                    return transaction = new Transaction(this, Connection.BeginTransaction(il));
                }

                if (transaction.IsolationLevel == il)
                {
                    return transaction;
                }

                throw new NotSupportedException("尚有未提交的事务！");
            }

            public void Close() => Connection.Close();

            public void ChangeDatabase(string databaseName) => Connection.ChangeDatabase(databaseName);

            public IDbCommand CreateCommand()
            {
                var command = Connection.CreateCommand();

                command.Transaction = transaction?.DbTransaction;

                return command;
            }

            public void Open() => Connection.Open();

            public void Dispose() => Connection.Dispose();
        }
    }
}
