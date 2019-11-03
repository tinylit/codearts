using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using Transaction = System.Transactions.Transaction;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 事务连接池
    /// </summary>
    public static class TransactionConnections
    {
        /// <summary>
        /// 事务连接
        /// </summary>
        private class TransactionConnection : DbConnection
        {
            private int refCount = 0;
            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="connection">源链接</param>
            public TransactionConnection(IDbConnection connection) : base(connection)
            {
            }

            /// <summary>
            /// 获取链接
            /// </summary>
            /// <returns></returns>
            public IDbConnection GetConnection()
            {
                Interlocked.Increment(ref refCount);

                return this;
            }

            public override void Close() { }

            protected override void Dispose(bool disposing)
            {
                if (disposing && Interlocked.Decrement(ref refCount) == 0)
                {
                    base.Dispose(disposing);
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

            if (current is null) return null;

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
            transactionConnections.TryRemove(e.Transaction, out Dictionary<string, TransactionConnection> dictionary);

            if (dictionary == null) return;

            lock (dictionary)
            {
                foreach (TransactionConnection value in dictionary.Values)
                {
                    value.Dispose();
                }
            }
        }
    }
}
