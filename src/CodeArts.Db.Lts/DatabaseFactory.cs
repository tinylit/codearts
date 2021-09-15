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
            public void Rollback() => DbTransaction.Rollback();

            private bool disposedValue = false;

            public void Dispose()
            {
                if (!disposedValue)
                {
                    database.CheckTransaction(this);

                    disposedValue = true;
                }

                DbTransaction.Dispose();
            }
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

            public string Format(SQL sql) => sql?.ToString(adapter.Settings);

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

            public void Close() => connection?.Close();

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

            public void Dispose() => connection?.Dispose();

            public T Read<T>(Expression expression)
            {
                return Read(databaseFor.Read<T>(expression));
            }

            public IEnumerable<T> Query<T>(Expression expression)
            {
                return Query<T>(databaseFor.Read<T>(expression));
            }

            public int Execute(Expression expression)
            {
                return Execute(databaseFor.Execute(expression));
            }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            public Task<T> ReadAsync<T>(Expression expression, CancellationToken cancellationToken = default)
            {
                return ReadAsync(databaseFor.Read<T>(expression), cancellationToken);
            }

            public IAsyncEnumerable<T> QueryAsync<T>(Expression expression)
            {
                return QueryAsync<T>(databaseFor.Read<T>(expression));
            }

            public Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
            {
                return ExecuteAsync(databaseFor.Execute(expression), cancellationToken);
            }
#endif

            public T Read<T>(CommandSql<T> commandSql)
            {
                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.Read<T>(dbConnection, commandSql);
                    }
                }

                if (transactions.Count == 0)
                {
                    return databaseFor.Read<T>(connection, commandSql);
                }

                return databaseFor.Read<T>(this, commandSql);
            }

            public T Single<T>(string sql, object param = null, string missingMsg = null, int? commandTimeout = null)
            {
                return Read(new CommandSql<T>(sql, param, RowStyle.Single, missingMsg, commandTimeout));
            }

            public T SingleOrDefault<T>(string sql, object param = null, int? commandTimeout = null, T defaultValue = default)
            {
                return Read(new CommandSql<T>(sql, param, RowStyle.SingleOrDefault, defaultValue, commandTimeout));
            }

            public T First<T>(string sql, object param = null, string missingMsg = null, int? commandTimeout = null)
            {
                return Read(new CommandSql<T>(sql, param, RowStyle.First, missingMsg, commandTimeout));
            }

            public T FirstOrDefault<T>(string sql, object param = null, int? commandTimeout = null, T defaultValue = default)
            {
                return Read(new CommandSql<T>(sql, param, RowStyle.FirstOrDefault, defaultValue, commandTimeout));
            }

            public IEnumerable<T> Query<T>(CommandSql commandSql)
            {
                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.Query<T>(dbConnection, commandSql);
                    }
                }

                if (transactions.Count == 0)
                {
                    return databaseFor.Query<T>(connection, commandSql);
                }

                return databaseFor.Query<T>(this, commandSql);
            }

            public IEnumerable<T> Query<T>(string sql, object param = null, int? commandTimeout = null)
            {
                return Query<T>(new CommandSql(sql, param, commandTimeout));
            }

            public int Execute(CommandSql commandSql)
            {
                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.Execute(dbConnection, commandSql);
                    }
                }

                if (transactions.Count == 0)
                {
                    return databaseFor.Execute(connection, commandSql);
                }

                return databaseFor.Execute(this, commandSql);
            }

            public int Execute(string sql, object param = null, int? commandTimeout = null)
            {
                return Execute(new CommandSql(sql, param, commandTimeout));
            }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER

            public Task<T> ReadAsync<T>(CommandSql<T> commandSql, CancellationToken cancellationToken = default)
            {
                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.ReadAsync(dbConnection, commandSql, cancellationToken);
                    }
                }

                if (transactions.Count == 0)
                {
                    return databaseFor.ReadAsync(connection, commandSql, cancellationToken);
                }

                return databaseFor.ReadAsync(this, commandSql, cancellationToken);
            }

            public Task<T> SingleAsync<T>(string sql, object param = null, string missingMsg = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
            {
                return ReadAsync(new CommandSql<T>(sql, param, RowStyle.Single, missingMsg, commandTimeout), cancellationToken);
            }

            public Task<T> SingleOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, T defaultValue = default, CancellationToken cancellationToken = default)
            {
                return ReadAsync(new CommandSql<T>(sql, param, RowStyle.SingleOrDefault, defaultValue, commandTimeout), cancellationToken);
            }

            public Task<T> FirstAsync<T>(string sql, object param = null, string missingMsg = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
            {
                return ReadAsync(new CommandSql<T>(sql, param, RowStyle.First, missingMsg, commandTimeout), cancellationToken);
            }

            public Task<T> FirstOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, T defaultValue = default, CancellationToken cancellationToken = default)
            {
                return ReadAsync(new CommandSql<T>(sql, param, RowStyle.FirstOrDefault, defaultValue, commandTimeout), cancellationToken);
            }

            public IAsyncEnumerable<T> QueryAsync<T>(CommandSql commandSql)
            {
                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.QueryAsync<T>(dbConnection, commandSql);
                    }
                }

                if (transactions.Count == 0)
                {
                    return databaseFor.QueryAsync<T>(connection, commandSql);
                }

                return databaseFor.QueryAsync<T>(this, commandSql);
            }

            public IAsyncEnumerable<T> QueryAsync<T>(string sql, object param = null, int? commandTimeout = null)
            {
                return QueryAsync<T>(new CommandSql(sql, param, commandTimeout));
            }

            public Task<int> ExecuteAsync(CommandSql commandSql, CancellationToken cancellationToken = default)
            {
                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.ExecuteAsync(dbConnection, commandSql, cancellationToken);
                    }
                }

                if (transactions.Count == 0)
                {
                    return databaseFor.ExecuteAsync(connection, commandSql, cancellationToken);
                }

                return databaseFor.ExecuteAsync(this, commandSql, cancellationToken);
            }

            public Task<int> ExecuteAsync(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
            {
                return ExecuteAsync(new CommandSql(sql, param, commandTimeout), cancellationToken);
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

            private bool disposedValue = false;

            protected override void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        database.CheckTransaction(this);
                    }

                    disposedValue = true;
                }

                Transaction.Dispose();
            }

#if NETSTANDARD2_1_OR_GREATER
            public override Task CommitAsync(CancellationToken cancellationToken = default) => Transaction.CommitAsync(cancellationToken);
            public override Task RollbackAsync(CancellationToken cancellationToken = default) => Transaction.RollbackAsync(cancellationToken);

            public override ValueTask DisposeAsync()
            {
                if (!disposedValue)
                {
                    database.CheckTransaction(this);

                    disposedValue = true;
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

            public string Format(SQL sql) => sql?.ToString(adapter.Settings);

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

            public override void Close() => connection.Close();

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
            public override Task CloseAsync() => connection.CloseAsync();
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
            
            public T Read<T>(Expression expression)
            {
                return Read(databaseFor.Read<T>(expression));
            }

            public IEnumerable<T> Query<T>(Expression expression)
            {
                return Query<T>(databaseFor.Read<T>(expression));
            }

            public int Execute(Expression expression)
            {
                return Execute(databaseFor.Execute(expression));
            }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            public Task<T> ReadAsync<T>(Expression expression, CancellationToken cancellationToken = default)
            {
                return ReadAsync(databaseFor.Read<T>(expression), cancellationToken);
            }

            public IAsyncEnumerable<T> QueryAsync<T>(Expression expression)
            {
                return QueryAsync<T>(databaseFor.Read<T>(expression));
            }

            public Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
            {
                return ExecuteAsync(databaseFor.Execute(expression), cancellationToken);
            }
#endif

            public T Read<T>(CommandSql<T> commandSql)
            {
                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.Read<T>(dbConnection, commandSql);
                    }
                }

                if (transactions.Count == 0)
                {
                    return databaseFor.Read<T>(connection, commandSql);
                }

                return databaseFor.Read<T>(this, commandSql);
            }

            public T Single<T>(string sql, object param = null, string missingMsg = null, int? commandTimeout = null)
            {
                return Read(new CommandSql<T>(sql, param, RowStyle.Single, missingMsg, commandTimeout));
            }

            public T SingleOrDefault<T>(string sql, object param = null, int? commandTimeout = null, T defaultValue = default)
            {
                return Read(new CommandSql<T>(sql, param, RowStyle.SingleOrDefault, defaultValue, commandTimeout));
            }

            public T First<T>(string sql, object param = null, string missingMsg = null, int? commandTimeout = null)
            {
                return Read(new CommandSql<T>(sql, param, RowStyle.First, missingMsg, commandTimeout));
            }

            public T FirstOrDefault<T>(string sql, object param = null, int? commandTimeout = null, T defaultValue = default)
            {
                return Read(new CommandSql<T>(sql, param, RowStyle.FirstOrDefault, defaultValue, commandTimeout));
            }

            public IEnumerable<T> Query<T>(CommandSql commandSql)
            {
                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.Query<T>(dbConnection, commandSql);
                    }
                }

                if (transactions.Count == 0)
                {
                    return databaseFor.Query<T>(connection, commandSql);
                }

                return databaseFor.Query<T>(this, commandSql);
            }

            public IEnumerable<T> Query<T>(string sql, object param = null, int? commandTimeout = null)
            {
                return Query<T>(new CommandSql(sql, param, commandTimeout));
            }

            public int Execute(CommandSql commandSql)
            {
                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.Execute(dbConnection, commandSql);
                    }
                }

                if (transactions.Count == 0)
                {
                    return databaseFor.Execute(connection, commandSql);
                }

                return databaseFor.Execute(this, commandSql);
            }

            public int Execute(string sql, object param = null, int? commandTimeout = null)
            {
                return Execute(new CommandSql(sql, param, commandTimeout));
            }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER

            public Task<T> ReadAsync<T>(CommandSql<T> commandSql, CancellationToken cancellationToken = default)
            {
                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.ReadAsync(dbConnection, commandSql, cancellationToken);
                    }
                }

                if (transactions.Count == 0)
                {
                    return databaseFor.ReadAsync(connection, commandSql, cancellationToken);
                }

                return databaseFor.ReadAsync(this, commandSql, cancellationToken);
            }

            public Task<T> SingleAsync<T>(string sql, object param = null, string missingMsg = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
            {
                return ReadAsync(new CommandSql<T>(sql, param, RowStyle.Single, missingMsg, commandTimeout), cancellationToken);
            }

            public Task<T> SingleOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, T defaultValue = default, CancellationToken cancellationToken = default)
            {
                return ReadAsync(new CommandSql<T>(sql, param, RowStyle.SingleOrDefault, defaultValue, commandTimeout), cancellationToken);
            }

            public Task<T> FirstAsync<T>(string sql, object param = null, string missingMsg = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
            {
                return ReadAsync(new CommandSql<T>(sql, param, RowStyle.First, missingMsg, commandTimeout), cancellationToken);
            }

            public Task<T> FirstOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, T defaultValue = default, CancellationToken cancellationToken = default)
            {
                return ReadAsync(new CommandSql<T>(sql, param, RowStyle.FirstOrDefault, defaultValue, commandTimeout), cancellationToken);
            }

            public IAsyncEnumerable<T> QueryAsync<T>(CommandSql commandSql)
            {
                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.QueryAsync<T>(dbConnection, commandSql);
                    }
                }

                if (transactions.Count == 0)
                {
                    return databaseFor.QueryAsync<T>(connection, commandSql);
                }

                return databaseFor.QueryAsync<T>(this, commandSql);
            }

            public IAsyncEnumerable<T> QueryAsync<T>(string sql, object param = null, int? commandTimeout = null)
            {
                return QueryAsync<T>(new CommandSql(sql, param, commandTimeout));
            }

            public Task<int> ExecuteAsync(CommandSql commandSql, CancellationToken cancellationToken = default)
            {
                if (connection is null)
                {
                    using (var dbConnection = TransactionConnections.GetConnection(connectionConfig.ConnectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, adapter, true))
                    {
                        return databaseFor.ExecuteAsync(dbConnection, commandSql, cancellationToken);
                    }
                }

                if (transactions.Count == 0)
                {
                    return databaseFor.ExecuteAsync(connection, commandSql, cancellationToken);
                }

                return databaseFor.ExecuteAsync(this, commandSql, cancellationToken);
            }

            public Task<int> ExecuteAsync(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
            {
                return ExecuteAsync(new CommandSql(sql, param, commandTimeout), cancellationToken);
            }
#endif
        }
    }
}
