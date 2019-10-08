using System;
using System.Collections.Generic;
using System.Data;
using System.Transactions;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 事务连接池
    /// </summary>
    public static class TransactionScopeConnections
    {
        private static readonly Dictionary<Transaction, Dictionary<string, DbConnectionWrapper>> transactionConnections = new Dictionary<Transaction, Dictionary<string, DbConnectionWrapper>>();
        public static IDbConnection GetConnection(string connectionString, IDbConnectionAdapter adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }
            Transaction current = Transaction.Current;
            if (current == null) return null;
            Dictionary<string, DbConnectionWrapper> dictionary = null;
            lock (transactionConnections)
            {
                if (!transactionConnections.TryGetValue(current, out dictionary))
                {
                    transactionConnections.Add(current, dictionary = new Dictionary<string, DbConnectionWrapper>());
                    current.TransactionCompleted += OnTransactionCompleted;
                }
            }
            DbConnectionWrapper dbconnection = null;
            lock (dictionary)
            {
                if (!dictionary.TryGetValue(connectionString, out dbconnection))
                {
                    dictionary.Add(connectionString, dbconnection = new DbConnectionWrapper(adapter.Create(connectionString)));
                }
                return dbconnection.GetConnection();
            }
        }

        private static void OnTransactionCompleted(object sender, TransactionEventArgs e)
        {
            Dictionary<string, DbConnectionWrapper> dictionary = null;
            lock (transactionConnections)
            {
                if (transactionConnections.TryGetValue(e.Transaction, out dictionary))
                {
                    transactionConnections.Remove(e.Transaction);
                }
            }
            if (dictionary == null) return;

            lock (dictionary)
            {
                foreach (DbConnectionWrapper value in dictionary.Values)
                {
                    value.Dispose();
                }
            }
        }
    }
}
