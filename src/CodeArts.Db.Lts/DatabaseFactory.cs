using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

            public void Commit() => DbTransaction.Commit();

            private bool isDisposed = false;

            public void Dispose()
            {
                if (!isDisposed)
                {
                    database.CheckTransaction(this);

                    DbTransaction.Dispose();

                    isDisposed = true;
                }
            }

            public void Rollback() => DbTransaction.Rollback();
        }

        private class Database : IDatabase
        {
            private IDbConnection connection;
            private readonly IDatabaseFor databaseFor;
            private readonly IDbConnectionLtsAdapter adapter;
            private readonly IReadOnlyConnectionConfig connectionConfig;
            private readonly Stack<Transaction> transactions = new Stack<Transaction>();

            public Database(IReadOnlyConnectionConfig connectionConfig, IDbConnectionLtsAdapter adapter)
            {
                this.connectionConfig = connectionConfig;
                this.adapter = adapter;
                this.databaseFor = DbConnectionManager.GetOrCreate(adapter);
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

            public void CheckTransaction(Transaction transaction)
            {
                if (transactions.Count == 0 || !ReferenceEquals(transactions.Pop(), transaction))
                {
                    throw new DException("事务未有序释放！");
                }
            }

            public IDbTransaction BeginTransaction()
            {
                var transaction = new Transaction(this, Connection.BeginTransaction());

                transactions.Push(transaction);

                return transaction;
            }

            public IDbTransaction BeginTransaction(IsolationLevel il)
            {
                var transaction = new Transaction(this, Connection.BeginTransaction(il));

                transactions.Push(transaction);

                return transaction;
            }

            public void Close() => Connection.Close();

            public void ChangeDatabase(string databaseName) => Connection.ChangeDatabase(databaseName);

            public IDbCommand CreateCommand()
            {
                var command = Connection.CreateCommand();

                if (transactions.Count > 0)
                {
                    var transaction = transactions.Pop();

                    command.Transaction = transaction.DbTransaction;
                }

                return command;
            }

            public void Open() => Connection.Open();

            public void Dispose() => Connection.Dispose();

            public T Single<T>(Expression expression)
            {
                var commandSql = databaseFor.Read<T>(expression);

                SqlCapture.Current?.Capture(commandSql);

                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.Single(dbConnection, commandSql);
                    }
                }

                return databaseFor.Single(this, commandSql);
            }

            public IEnumerable<T> Query<T>(Expression expression)
            {
                var commandSql = databaseFor.Read<T>(expression);

                SqlCapture.Current?.Capture(commandSql);

                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.Query<T>(dbConnection, commandSql);
                    }
                }

                return databaseFor.Query<T>(this, commandSql);
            }

            public int Execute(Expression expression)
            {
                var commandSql = databaseFor.Execute(expression);

                SqlCapture.Current?.Capture(commandSql);

                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.Execute(dbConnection, commandSql);
                    }
                }

                return databaseFor.Execute(this, commandSql);
            }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            public Task<T> SingleAsync<T>(Expression expression, CancellationToken cancellationToken = default)
            {
                var commandSql = databaseFor.Read<T>(expression);

                SqlCapture.Current?.Capture(commandSql);

                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.SingleAsync(dbConnection, commandSql, cancellationToken);
                    }
                }

                return databaseFor.SingleAsync(this, commandSql, cancellationToken);
            }

            public IAsyncEnumerable<T> QueryAsync<T>(Expression expression)
            {
                var commandSql = databaseFor.Read<T>(expression);

                SqlCapture.Current?.Capture(commandSql);

                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.QueryAsync<T>(dbConnection, commandSql);
                    }
                }

                return databaseFor.QueryAsync<T>(this, commandSql);
            }

            public Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
            {
                var commandSql = databaseFor.Execute(expression);

                SqlCapture.Current?.Capture(commandSql);

                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.ExecuteAsync(dbConnection, commandSql, cancellationToken);
                    }
                }

                return databaseFor.ExecuteAsync(this, commandSql, cancellationToken);
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

            public override void Commit() => Transaction.Commit();

            public override void Rollback() => Transaction.Rollback();

            private bool isDisposed = false;

            protected override void Dispose(bool disposing)
            {
                if (disposing && !isDisposed)
                {
                    database.CheckTransaction(this);

                    isDisposed = true;
                }

                Transaction.Dispose();

                base.Dispose(disposing);
            }

#if NETSTANDARD2_1_OR_GREATER
            public override Task CommitAsync(CancellationToken cancellationToken = default)
            {
                return base.CommitAsync(cancellationToken);
            }
            public override Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                return base.RollbackAsync(cancellationToken);
            }

            public override ValueTask DisposeAsync()
            {
                if (!isDisposed)
                {
                    database.CheckTransaction(this);

                    isDisposed = true;
                }

                return Transaction.DisposeAsync();
            }
#endif
        }

        private class DbDatabase : System.Data.Common.DbConnection, IDatabase
        {
            private System.Data.Common.DbConnection connection;
            private readonly IDatabaseFor databaseFor;
            private readonly IDbConnectionLtsAdapter adapter;
            private readonly IReadOnlyConnectionConfig connectionConfig;
            private readonly Stack<DbTransaction> transactions = new Stack<DbTransaction>();

            public DbDatabase(IReadOnlyConnectionConfig connectionConfig, IDbConnectionLtsAdapter adapter)
            {
                this.connectionConfig = connectionConfig;
                this.adapter = adapter;
                this.databaseFor = DbConnectionManager.GetOrCreate(adapter);
            }

            public string Name => connectionConfig.Name;

            public string ProviderName => adapter.ProviderName;

            public ISQLCorrectSettings Settings => adapter.Settings;

            public override string ConnectionString { get => Connection.ConnectionString; set => Connection.ConnectionString = value; }

            public override string Database => Connection.Database;

            public override string DataSource => Connection.DataSource;

            public override string ServerVersion => Connection.ServerVersion;

            public override ConnectionState State => Connection.State;

            public override ISite Site { get => Connection.Site; set => Connection.Site = value; }

#if NETSTANDARD2_1_OR_GREATER
            private System.Data.Common.DbConnection Connection => connection ??= (System.Data.Common.DbConnection)(TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true));
#else
            private System.Data.Common.DbConnection Connection => connection ?? (connection = (System.Data.Common.DbConnection)(TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true)));
#endif

            public override void ChangeDatabase(string databaseName) => Connection.ChangeDatabase(databaseName);

            public override void Close() => Connection.Close();

            public override void Open() => Connection.Open();

            public void CheckTransaction(DbTransaction transaction)
            {
                if (transactions.Count == 0 || !ReferenceEquals(transactions.Pop(), transaction))
                {
                    throw new DException("事务未有序释放！");
                }
            }

            protected override System.Data.Common.DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            {
                var transaction = new DbTransaction(this, connection.BeginTransaction(isolationLevel));

                transactions.Push(transaction);

                return transaction;
            }

            protected override System.Data.Common.DbCommand CreateDbCommand()
            {
                var command = Connection.CreateCommand();

                if (transactions.Count > 0)
                {
                    var transaction = transactions.Peek();

                    command.Transaction = transaction.Transaction;
                }

                return command;
            }

            public override DataTable GetSchema(string collectionName) => Connection.GetSchema(collectionName);

            public override DataTable GetSchema(string collectionName, string[] restrictionValues) => Connection.GetSchema(collectionName, restrictionValues);

            public override DataTable GetSchema() => Connection.GetSchema();

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            public override Task OpenAsync(CancellationToken cancellationToken) => Connection.OpenAsync(cancellationToken);
#endif

#if NETSTANDARD2_1_OR_GREATER
            public override ValueTask DisposeAsync() => connection?.DisposeAsync() ?? new ValueTask();
            public override Task CloseAsync() => Connection.CloseAsync();
            public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default) => Connection.ChangeDatabaseAsync(databaseName, cancellationToken);
            protected override async ValueTask<System.Data.Common.DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
            {
                var transaction = new DbTransaction(this, await Connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false));

                transactions.Push(transaction);

                return transaction;
            }
#endif

            public override event StateChangeEventHandler StateChange
            {
                add { Connection.StateChange += value; }
                remove { Connection.StateChange -= value; }
            }

            public override void EnlistTransaction(System.Transactions.Transaction transaction) => Connection.EnlistTransaction(transaction);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    connection?.Dispose();
                }

                base.Dispose(disposing);
            }

            public T Single<T>(Expression expression)
            {
                var commandSql = databaseFor.Read<T>(expression);

                SqlCapture.Current?.Capture(commandSql);

                return databaseFor.Single(this, commandSql);
            }

            public IEnumerable<T> Query<T>(Expression expression)
            {
                var commandSql = databaseFor.Read<T>(expression);

                SqlCapture.Current?.Capture(commandSql);

                return databaseFor.Query<T>(this, commandSql);
            }

            public int Execute(Expression expression)
            {
                var commandSql = databaseFor.Execute(expression);

                SqlCapture.Current?.Capture(commandSql);

                return databaseFor.Execute(this, commandSql);
            }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            public Task<T> SingleAsync<T>(Expression expression, CancellationToken cancellationToken = default)
            {
                var commandSql = databaseFor.Read<T>(expression);

                SqlCapture.Current?.Capture(commandSql);

                return databaseFor.SingleAsync(this, commandSql, cancellationToken);
            }

            public IAsyncEnumerable<T> QueryAsync<T>(Expression expression)
            {
                var commandSql = databaseFor.Read<T>(expression);

                SqlCapture.Current?.Capture(commandSql);

                return databaseFor.QueryAsync<T>(this, commandSql);
            }

            public Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
            {
                var commandSql = databaseFor.Execute(expression);

                SqlCapture.Current?.Capture(commandSql);

                return databaseFor.ExecuteAsync(this, commandSql, cancellationToken);
            }
#endif
        }
    }
}
