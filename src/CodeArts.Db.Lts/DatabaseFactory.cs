using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
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
                return new DbDatabase(connectionConfig, adapter);
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

        private class Database : IDatabase
        {
            private Transaction transaction;
            private IDbConnection connection;
            private readonly IDbConnectionLtsAdapter adapter;
            private readonly IReadOnlyConnectionConfig connectionConfig;

            public Database(IReadOnlyConnectionConfig connectionConfig, IDbConnectionLtsAdapter adapter)
            {
                this.connectionConfig = connectionConfig;
                this.adapter = adapter;
            }

            public string Name => connectionConfig.Name;

            public string ProviderName => adapter.ProviderName;

            public ISQLCorrectSettings Settings => adapter.Settings;

            public string ConnectionString { get => Connection.ConnectionString; set => Connection.ConnectionString = value; }

            public int ConnectionTimeout => Connection.ConnectionTimeout;

            string IDbConnection.Database => Connection.Database;

            public ConnectionState State => Connection.State;

#if NETSTANDARD2_1_OR_GREATER
            private IDbConnection Connection => connection ??= TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true);
#else
            private IDbConnection Connection => connection ?? (connection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true));
#endif

            public void ResetTransaction() => transaction = null;

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

            public T Read<T>(Expression expression)
            {
                return DbConnectionManager.Create(adapter).Read<T>(this, expression);
            }

            public IEnumerable<T> Query<T>(Expression expression)
            {
                return DbConnectionManager.Create(adapter).Query<T>(this, expression);
            }

            public int Execute(Expression expression)
            {
                return DbConnectionManager.Create(adapter).Execute(this, expression);
            }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            public Task<T> ReadAsync<T>(Expression expression, CancellationToken cancellationToken = default)
            {
                return DbConnectionManager.Create(adapter).ReadAsync<T>(this, expression, cancellationToken);
            }

            public IAsyncEnumerable<T> QueryAsync<T>(Expression expression)
            {
                return DbConnectionManager.Create(adapter).QueryAsync<T>(this, expression);
            }

            public Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
            {
                return DbConnectionManager.Create(adapter).ExecuteAsync(this, expression, cancellationToken);
            }
#endif
        }

        private class DbTransaction : System.Data.Common.DbTransaction
        {
            private readonly DbDatabase database;

            public DbTransaction(DbDatabase database, System.Data.Common.DbTransaction transaction)
            {
                this.database = database;
                Transaction = transaction;
            }

            public System.Data.Common.DbTransaction Transaction { get; }

            public override IsolationLevel IsolationLevel => Transaction.IsolationLevel;

            protected override System.Data.Common.DbConnection DbConnection => database;

            public override void Commit()
            {
                database.ResetTransaction();

                Transaction.Commit();
            }

            public override void Rollback()
            {
                database.ResetTransaction();

                Transaction.Rollback();
            }
            protected override void Dispose(bool disposing)
            {
                database.ResetTransaction();

                if (disposing)
                {
                    Transaction.Dispose();
                }

                base.Dispose(disposing);
            }

#if NETSTANDARD2_1_OR_GREATER
            public override Task CommitAsync(CancellationToken cancellationToken = default)
            {
                database.ResetTransaction();

                return base.CommitAsync(cancellationToken);
            }
            public override Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                database.ResetTransaction();

                return base.RollbackAsync(cancellationToken);
            }

            public override ValueTask DisposeAsync()
            {
                database.ResetTransaction();

                return base.DisposeAsync();
            }
#endif
        }

        private class DbDatabase : System.Data.Common.DbConnection, IDatabase
        {
            private System.Data.Common.DbConnection connection;
            private DbTransaction transaction;
            private readonly IDbConnectionLtsAdapter adapter;
            private readonly IReadOnlyConnectionConfig connectionConfig;

            public DbDatabase(IReadOnlyConnectionConfig connectionConfig, IDbConnectionLtsAdapter adapter)
            {
                this.connectionConfig = connectionConfig;
                this.adapter = adapter;
            }

            public string Name => connectionConfig.Name;

            public string ProviderName => adapter.ProviderName;

            public ISQLCorrectSettings Settings => adapter.Settings;

            public override string ConnectionString { get => Connection.ConnectionString; set => Connection.ConnectionString = value; }

            public override string Database => Connection.Database;

            public override string DataSource => Connection.DataSource;

            public override string ServerVersion => Connection.ServerVersion;

            public override ConnectionState State => Connection.State;

#if NETSTANDARD2_1_OR_GREATER
            private System.Data.Common.DbConnection Connection => connection ??= (System.Data.Common.DbConnection)(TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true));
#else
            private System.Data.Common.DbConnection Connection => connection ?? (connection = (System.Data.Common.DbConnection)(TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true)));
#endif

            public override void ChangeDatabase(string databaseName) => Connection.ChangeDatabase(databaseName);

            public override void Close() => Connection.Close();

            public override void Open() => Connection.Open();

            public void ResetTransaction() => transaction = null;

            protected override System.Data.Common.DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            {
                if (transaction is null)
                {
                    return transaction = new DbTransaction(this, connection.BeginTransaction(isolationLevel));
                }

                if (transaction.IsolationLevel == isolationLevel)
                {
                    return transaction;
                }

                throw new NotSupportedException("尚有未提交的事务！");
            }

            protected override System.Data.Common.DbCommand CreateDbCommand()
            {
                var command = Connection.CreateCommand();

                command.Transaction = transaction?.Transaction;

                return command;
            }

            public override DataTable GetSchema(string collectionName)
            {
                return Connection.GetSchema(collectionName);
            }

            public override DataTable GetSchema(string collectionName, string[] restrictionValues)
            {
                return Connection.GetSchema(collectionName, restrictionValues);
            }

            public override DataTable GetSchema()
            {
                return Connection.GetSchema();
            }


            public override event StateChangeEventHandler StateChange
            {
                add { Connection.StateChange += value; }
                remove { Connection.StateChange -= value; }
            }

            public override void EnlistTransaction(System.Transactions.Transaction transaction)
            {
                Connection.EnlistTransaction(transaction);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    connection?.Dispose();
                }

                base.Dispose(disposing);
            }
            
            public T Read<T>(Expression expression)
            {
                return DbConnectionManager.Create(adapter).Read<T>(this, expression);
            }

            public IEnumerable<T> Query<T>(Expression expression)
            {
                return DbConnectionManager.Create(adapter).Query<T>(this, expression);
            }

            public int Execute(Expression expression)
            {
                return DbConnectionManager.Create(adapter).Execute(this, expression);
            }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            public Task<T> ReadAsync<T>(Expression expression, CancellationToken cancellationToken = default)
            {
                return DbConnectionManager.Create(adapter).ReadAsync<T>(this, expression, cancellationToken);
            }

            public IAsyncEnumerable<T> QueryAsync<T>(Expression expression)
            {
                return DbConnectionManager.Create(adapter).QueryAsync<T>(this, expression);
            }

            public Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
            {
                return DbConnectionManager.Create(adapter).ExecuteAsync(this, expression, cancellationToken);
            }
#endif
        }
    }
}
