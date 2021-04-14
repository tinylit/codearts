using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
#if NET40 ||NET_NORMAL
using System.Runtime.Remoting;
#endif
using System.Threading;
using System.Threading.Tasks;
using Transaction = System.Transactions.Transaction;

namespace CodeArts.Db
{
    /// <summary>
    /// 事务连接池。
    /// </summary>
    public static class TransactionConnections
    {
        private interface ITransactionConnection : IDbConnection
        {
            /// <summary>
            /// 释放。
            /// </summary>
            void Destroy();
        }
        private class DbConnection : ITransactionConnection
        {
            private readonly IDbConnection connection;

            public string ConnectionString { get => connection.ConnectionString; set => connection.ConnectionString = value; }

            public int ConnectionTimeout => connection.ConnectionTimeout;

            public string Database => connection.Database;

            public ConnectionState State => connection.State;

            public DbConnection(IDbConnection connection)
            {
                this.connection = connection;
            }

            public void Close() { }

            public IDbTransaction BeginTransaction() => connection.BeginTransaction();

            public IDbTransaction BeginTransaction(IsolationLevel il) => connection.BeginTransaction(il);

            public void ChangeDatabase(string databaseName) => connection.ChangeDatabase(databaseName);

            public IDbCommand CreateCommand() => connection.CreateCommand();

            public void Open()
            {
                switch (State)
                {
                    case ConnectionState.Closed:
                        connection.Open();
                        break;
                    case ConnectionState.Connecting:

                        do
                        {
                            Thread.Sleep(5);

                        } while (State == ConnectionState.Connecting);

                        goto default;
                    case ConnectionState.Broken:
                        connection.Close();
                        goto default;
                    default:
                        if (connection.State == ConnectionState.Closed)
                        {
                            connection.Open();
                        }
                        break;
                }
            }

            public void Dispose() { }

            protected void Dispose(bool disposing)
            {
                if (disposing)
                {
                    connection?.Close();
                    connection?.Dispose();

                    GC.SuppressFinalize(this);
                }
            }

            public void Destroy() => Dispose(true);
        }
        private class TransactionConnection : System.Data.Common.DbConnection, ITransactionConnection
        {
            private readonly System.Data.Common.DbConnection connection;

            public override string ConnectionString { get => connection.ConnectionString; set => connection.ConnectionString = value; }

            public override string Database => connection.Database;

            public override string DataSource => connection.DataSource;

            public override string ServerVersion => connection.ServerVersion;


            public override int ConnectionTimeout => connection.ConnectionTimeout;

#if NET40 ||NET_NORMAL
            public override ObjRef CreateObjRef(Type requestedType) => connection.CreateObjRef(requestedType);
#endif

            public override object InitializeLifetimeService() => connection.InitializeLifetimeService();

            public override DataTable GetSchema() => connection.GetSchema();

            public override DataTable GetSchema(string collectionName) => connection.GetSchema(collectionName);

            public override DataTable GetSchema(string collectionName, string[] restrictionValues) => connection.GetSchema(collectionName, restrictionValues);

            public override ISite Site { get => connection.Site; set => connection.Site = value; }


            public override event StateChangeEventHandler StateChange { add { connection.StateChange += value; } remove { connection.StateChange -= value; } }

            public override void EnlistTransaction(Transaction transaction) => connection.EnlistTransaction(transaction);

            public override ConnectionState State => connection.State;

            public TransactionConnection(System.Data.Common.DbConnection connection)
            {
                this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            }

#if NET_NORMAL || NET_CORE
            public override async Task OpenAsync(CancellationToken cancellationToken)
            {
                switch (State)
                {
                    case ConnectionState.Closed:
                        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        break;
                    case ConnectionState.Connecting:
                        do
                        {
                            await Task.Delay(5, cancellationToken).ConfigureAwait(false);

                        } while (State == ConnectionState.Connecting);

                        goto default;
                    case ConnectionState.Broken:
#if NETSTANDARD2_1
                        await connection.CloseAsync()
                            .ConfigureAwait(false);
#else
                        connection.Close();
#endif
                        goto default;
                    default:
                        if (connection.State == ConnectionState.Closed)
                        {
                            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        }
                        break;
                }
            }
#endif

#if NETSTANDARD2_1
            public override Task CloseAsync() => Task.CompletedTask;

            public override ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);

            protected override ValueTask<System.Data.Common.DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) => connection.BeginTransactionAsync(isolationLevel, cancellationToken);


            public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default) => connection.ChangeDatabaseAsync(databaseName, cancellationToken);

#endif

            void IDisposable.Dispose() { }

            public void Destroy() => Dispose(true);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    connection?.Close();
                    connection?.Dispose();

                    GC.SuppressFinalize(this);
                }

                base.Dispose(true);
            }

            protected override System.Data.Common.DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => connection.BeginTransaction(isolationLevel);

            public override void Close() { }

            public override void ChangeDatabase(string databaseName) => connection.ChangeDatabase(databaseName);

            protected override System.Data.Common.DbCommand CreateDbCommand() => connection.CreateCommand();

            public override void Open()
            {
                switch (State)
                {
                    case ConnectionState.Closed:
                        connection.Open();
                        break;
                    case ConnectionState.Connecting:

                        do
                        {
                            Thread.Sleep(5);

                        } while (State == ConnectionState.Connecting);

                        goto default;
                    case ConnectionState.Broken:
                        connection.Close();
                        goto default;
                    default:
                        if (connection.State == ConnectionState.Closed)
                        {
                            connection.Open();
                        }
                        break;
                }
            }
        }

        private static readonly ConcurrentDictionary<Transaction, Dictionary<string, ITransactionConnection>> transactionConnections = new ConcurrentDictionary<Transaction, Dictionary<string, ITransactionConnection>>();

        /// <summary>
        /// 获取数据库连接。
        /// </summary>
        /// <param name="connectionString">连接字符串。</param>
        /// <param name="factory">数据库工程。</param>
        /// <returns></returns>
        public static IDbConnection GetConnection(string connectionString, IDbConnectionFactory factory)
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            Transaction current = Transaction.Current;

            if (current is null)
            {
                return null;
            }

            Dictionary<string, ITransactionConnection> dictionary = transactionConnections.GetOrAdd(current, transaction =>
            {
                transaction.TransactionCompleted += OnTransactionCompleted;

                return new Dictionary<string, ITransactionConnection>();
            });

            if (dictionary.TryGetValue(connectionString, out ITransactionConnection info))
            {
                return info;
            }

            lock (dictionary)
            {
                if (dictionary.TryGetValue(connectionString, out info))
                {
                    return info;
                }

                var conn = DispatchConnections.Instance.GetConnection(connectionString, factory, false);

                if (conn is System.Data.Common.DbConnection dbConnection)
                {
                    info = new TransactionConnection(dbConnection);
                }
                else
                {
                    info = new DbConnection(conn);
                }

                dictionary.Add(connectionString, info);

                return info;
            }
        }

        private static void OnTransactionCompleted(object sender, System.Transactions.TransactionEventArgs e)
        {
            if (transactionConnections.TryRemove(e.Transaction, out Dictionary<string, ITransactionConnection> dictionary))
            {
                foreach (ITransactionConnection value in dictionary.Values)
                {
                    value.Destroy();
                }

                dictionary.Clear();
            }
        }
    }
}
