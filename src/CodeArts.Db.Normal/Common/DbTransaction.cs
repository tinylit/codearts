using System;
using System.Data;

namespace CodeArts.Db.Common
{
    /// <summary>
    /// 事务。
    /// </summary>
    public class DbTransaction : IDisposable
    {
         /// <inheritdoc />
        internal DbTransaction(IDbTransaction transaction)
        {
            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

         /// <inheritdoc />
        internal IDbTransaction Transaction { get; }

        /// <inheritdoc />
        public IsolationLevel IsolationLevel => Transaction.IsolationLevel;

        /// <inheritdoc />
        public void Commit() => Transaction.Commit();

        /// <inheritdoc />
        public void Rollback() => Transaction.Rollback();

        /// <inheritdoc />
        public void Dispose() => Transaction.Dispose();
    }
}
