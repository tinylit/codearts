using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
#if NET40_OR_GREATER
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
        private static readonly ConcurrentDictionary<Transaction, Dictionary<string, IDbConnection>> transactionConnections = new ConcurrentDictionary<Transaction, Dictionary<string, IDbConnection>>();

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

            Dictionary<string, IDbConnection> dictionary = transactionConnections.GetOrAdd(current, transaction =>
            {
                transaction.TransactionCompleted += OnTransactionCompleted;

                return new Dictionary<string, IDbConnection>();
            });

            if (dictionary.TryGetValue(connectionString, out IDbConnection info))
            {
                return info;
            }

            lock (dictionary)
            {
                if (!dictionary.TryGetValue(connectionString, out info))
                {
                    dictionary.Add(connectionString, info = DispatchConnections.Instance.GetConnection(connectionString, factory, false));
                }

                return info;
            }
        }

        private static void OnTransactionCompleted(object sender, System.Transactions.TransactionEventArgs e)
        {
            if (transactionConnections.TryRemove(e.Transaction, out Dictionary<string, IDbConnection> dictionary))
            {
                foreach (IDbConnection value in dictionary.Values)
                {
                    value.Dispose();
                }

                dictionary.Clear();
            }
        }
    }
}
