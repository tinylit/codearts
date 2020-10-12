using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using Transaction = System.Transactions.Transaction;

namespace CodeArts.ORM
{
    /// <summary>
    /// 事务连接池
    /// </summary>
    public static class TransactionConnections
    {
        /// <summary>
        /// 事务连接
        /// </summary>
        private class TransactionConnection : IDbConnection
        {
            private int refCount = 0;

            private readonly IDbConnection connection;

            public string ConnectionString { get => connection.ConnectionString; set => connection.ConnectionString = value; }

            public int ConnectionTimeout => connection.ConnectionTimeout;

            public string Database => connection.Database;

            public ConnectionState State => connection.State;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="connection">源链接</param>
            public TransactionConnection(IDbConnection connection)
            {
                this.connection = connection;
            }

            /// <summary> 获取连接 </summary>
            /// <returns></returns>
            public IDbConnection GetConnection()
            {
                Interlocked.Increment(ref refCount);

                return this;
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

            void IDisposable.Dispose() { }

            /// <summary>
            /// 释放内存
            /// </summary>
            public void Dispose() => Dispose(Interlocked.Decrement(ref refCount) == 0);

            /// <summary>
            /// 释放内存
            /// </summary>
            /// <param name="disposing">确认释放</param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    connection?.Close();
                    connection?.Dispose();

                    GC.SuppressFinalize(this);
                }
            }
        }

        private static readonly ConcurrentDictionary<Transaction, Dictionary<string, TransactionConnection>> transactionConnections = new ConcurrentDictionary<Transaction, Dictionary<string, TransactionConnection>>();
        /// <summary>
        /// 获取数据库连接
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="adapter">数据库适配器</param>
        /// <returns></returns>
        public static IDbConnection GetConnection(string connectionString, IDbConnectionAdapter adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            Transaction current = Transaction.Current;

            if (current is null)
            {
                return null;
            }

            Dictionary<string, TransactionConnection> dictionary = transactionConnections.GetOrAdd(current, transaction =>
            {
                transaction.TransactionCompleted += OnTransactionCompleted;

                return new Dictionary<string, TransactionConnection>();
            });

            lock (dictionary)
            {
                if (!dictionary.TryGetValue(connectionString, out TransactionConnection info))
                {
                    dictionary.Add(connectionString, info = new TransactionConnection(adapter.Create(connectionString)));
                }

                return info.GetConnection();
            }
        }

        private static void OnTransactionCompleted(object sender, System.Transactions.TransactionEventArgs e)
        {
            if (transactionConnections.TryRemove(e.Transaction, out Dictionary<string, TransactionConnection> dictionary))
            {
                foreach (TransactionConnection value in dictionary.Values)
                {
                    value.Dispose();
                }

                dictionary.Clear();
            }
        }
    }
}
