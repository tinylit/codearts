using System;
using System.Data;
#if NETSTANDARD2_1_OR_GREATER
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.Lts
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

#if NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// 事务。
    /// </summary>
    public class DbTransactionAsync : DbTransaction, IAsyncDisposable
    {
        private readonly System.Data.Common.DbTransaction transaction;

        /// <inheritdoc />
        internal DbTransactionAsync(System.Data.Common.DbTransaction transaction) : base(transaction)
        {
            this.transaction = transaction;
        }
        /// <inheritdoc />
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);

        /// <inheritdoc />
        public Task RollbackAsync(CancellationToken cancellationToken = default) => transaction.RollbackAsync(cancellationToken);

        /// <inheritdoc />
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
#endif
}
