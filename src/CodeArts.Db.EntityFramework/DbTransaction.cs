#if NET_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
#else
using System.Data.Entity;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
#if NET_NORMAL || NET_CORE
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// Represents the default implementation of the <see cref="IDbTransaction"/> interface.
    /// </summary>
    public class DbTransaction : IDbTransaction
    {
        private bool disposed = false;
        private readonly DbContext[] dbContexts;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbTransaction"/> class.
        /// </summary>
        /// <param name="repositories">The contexts.</param>
        /// <exception cref="ArgumentNullException">The parameter <paramref name="repositories"/> is null</exception>
        /// <exception cref="ArgumentException">The parameter <paramref name="repositories"/> contains the duplicate value</exception>
        public DbTransaction(params ILinqRepository[] repositories)
        {
            if (IsRepeat(repositories ?? throw new ArgumentNullException(nameof(repositories))))
            {
                throw new ArgumentException(nameof(repositories));
            }

            if (repositories.Length > 1)
            {
                this.dbContexts = Distinct(repositories.Select(x => x.DBContext), repositories.Length);
            }
            else
            {
                this.dbContexts = repositories.Select(x => x.DBContext).ToArray();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbTransaction"/> class.
        /// </summary>
        /// <param name="dbContexts">The contexts.</param>
        /// <exception cref="ArgumentNullException">The parameter <paramref name="dbContexts"/> is null</exception>
        /// <exception cref="ArgumentException">The parameter <paramref name="dbContexts"/> contains the duplicate value</exception>
        public DbTransaction(params DbContext[] dbContexts)
        {
            if (IsRepeat(dbContexts ?? throw new ArgumentNullException(nameof(dbContexts))))
            {
                throw new ArgumentException(nameof(dbContexts));
            }

            this.dbContexts = dbContexts;
        }

        private static bool IsRepeat(ILinqRepository[] repositories)
        {
            if (repositories.Length == 1)
            {
                return false;
            }

            var list = new List<ILinqRepository>(repositories.Length);

            foreach (var item in repositories)
            {
                if (list.Contains(item))
                {
                    list.Clear();

                    return true;
                }

                list.Add(item);
            }

            list.Clear();

            return false;
        }

        private static bool IsRepeat(DbContext[] dbContexts)
        {
            if (dbContexts.Length == 1)
            {
                return false;
            }

            var list = new List<DbContext>(dbContexts.Length);

            foreach (var item in dbContexts)
            {
                if (list.Contains(item))
                {
                    list.Clear();

                    return true;
                }

                list.Add(item);
            }

            list.Clear();

            return false;
        }

        private static DbContext[] Distinct(IEnumerable<DbContext> dbContexts, int capacity)
        {
            var contexts = new List<DbContext>(capacity);

            foreach (var context in dbContexts)
            {
                if (context is null)
                {
                    throw new ArgumentException();
                }

                if (contexts.Contains(context))
                {
                    continue;
                }

                contexts.Add(context);
            }

            return contexts.ToArray();
        }

#if NET_CORE
        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges is called after the changes have been sent successfully to the database.</param>
        /// <returns>The number of state entries written to the database.</returns>
        public int Commit(bool acceptAllChangesOnSuccess = false)
#else
        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        public int Commit()
#endif
        {
            int count = 0;
#if NET_CORE
            var list = new List<IDbContextTransaction>();
#else
            var list = new List<DbContextTransaction>();
#endif
            try
            {
                foreach (var context in dbContexts)
                {
                    list.Add(context.Database.BeginTransaction());

#if NET_CORE
                    count += context.SaveChanges(acceptAllChangesOnSuccess);
#else
                    count += context.SaveChanges();
#endif
                }

                foreach (var item in list)
                {
                    item.Commit();
                }
            }
            catch
            {
                foreach (var item in list)
                {
                    item.Rollback();
                }

                throw;
            }
            finally
            {
                foreach (var item in list)
                {
                    item.Dispose();
                }
            }
            return count;
        }

#if NET_CORE
        /// <summary>
        /// Saves all changes made in this context to the database with distributed transaction.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges is called after the changes have been sent successfully to the database.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous save operation. The task result contains the number of state entities written to database.</returns>
        public async Task<int> CommitAsync(bool acceptAllChangesOnSuccess = false, CancellationToken cancellationToken = default)
        {
            int count = 0;
            var list = new List<IDbContextTransaction>();
            try
            {
                foreach (var context in dbContexts)
                {
                    list.Add(await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false));

                    count += await context.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
                }

                foreach (var item in list)
                {
                    await item.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                foreach (var item in list)
                {
                    await item.RollbackAsync(cancellationToken).ConfigureAwait(false);
                }

                throw;
            }
            finally
            {
                foreach (var item in list)
                {
                    await item.DisposeAsync().ConfigureAwait(false);
                }
            }
            return count;
        }
#endif

#if NET_NORMAL || NET_CORE
        /// <summary>
        /// Saves all changes made in this context to the database with distributed transaction.
        /// </summary>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous save operation. The task result contains the number of state entities written to database.</returns>
        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            int count = 0;
#if NET_CORE
            var list = new List<IDbContextTransaction>();
#else
            var list = new List<DbContextTransaction>();
#endif
            try
            {
                foreach (var context in dbContexts)
                {
#if NET_CORE
                    list.Add(await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false));
#else
                    list.Add(context.Database.BeginTransaction());
#endif

                    count += await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var item in list)
                {
#if NET_CORE
                    await item.CommitAsync().ConfigureAwait(false);
#else
                    item.Commit();
#endif
                }
            }
            catch
            {
                foreach (var item in list)
                {
#if NET_CORE
                    await item.RollbackAsync().ConfigureAwait(false);
#else
                    item.Rollback();
#endif
                }

                throw;
            }
            finally
            {
                foreach (var item in list)
                {
#if NET_CORE
                    await item.DisposeAsync().ConfigureAwait(false);
#else
                    item.Dispose();
#endif
                }
            }
            return count;
        }
#endif

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    // dispose the db context.
                    Array.Clear(dbContexts, 0, dbContexts.Length);

                    disposed = true;
                }
            }
        }
    }
}