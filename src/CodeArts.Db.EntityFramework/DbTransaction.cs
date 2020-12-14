#if NETSTANDARD2_0
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
#else
using System.Data.Entity;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public DbTransaction(params ILinqRepository[] repositories)
        {
            if (repositories is null)
            {
                throw new ArgumentNullException(nameof(repositories));
            }

            this.dbContexts = repositories.Select(x => x.DBContext).ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbTransaction"/> class.
        /// </summary>
        /// <param name="dbContexts">The contexts.</param>
        public DbTransaction(params DbContext[] dbContexts)
        {
            this.dbContexts = dbContexts ?? throw new ArgumentNullException(nameof(dbContexts));
        }

#if NETSTANDARD2_0
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
#if NETSTANDARD2_0
            var list = new List<IDbContextTransaction>();
#else
            var list = new List<DbContextTransaction>();
#endif
            try
            {
                foreach (var context in dbContexts)
                {
                    list.Add(context.Database.BeginTransaction());

#if NETSTANDARD2_0
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

#if NETSTANDARD2_0
        /// <summary>
        /// Saves all changes made in this context to the database with distributed transaction.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges is called after the changes have been sent successfully to the database.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous save operation. The task result contains the number of state entities written to database.</returns>
        public async Task<int> CommitAsync(bool acceptAllChangesOnSuccess = false, CancellationToken cancellationToken = default)
        {
            int count = 0;
#if NETSTANDARD2_0
            var list = new List<IDbContextTransaction>();
#else
            var list = new List<DbContextTransaction>();
#endif
            try
            {
                foreach (var context in dbContexts)
                {
#if NETSTANDARD2_0
                    list.Add(await context.Database.BeginTransactionAsync(cancellationToken));
#else
                    list.Add(context.Database.BeginTransaction());
#endif

                    count += await context.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
                }

                foreach (var item in list)
                {
#if NETSTANDARD2_0
                    await item.CommitAsync(cancellationToken);
#else
                    item.Commit();
#endif
                }
            }
            catch
            {
                foreach (var item in list)
                {
#if NETSTANDARD2_0
                    await item.RollbackAsync(cancellationToken);
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
                    item.Dispose();
                }
            }
            return count;
        }
#endif

#if !NET40
        /// <summary>
        /// Saves all changes made in this context to the database with distributed transaction.
        /// </summary>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous save operation. The task result contains the number of state entities written to database.</returns>
        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            int count = 0;
#if NETSTANDARD2_0
            var list = new List<IDbContextTransaction>();
#else
            var list = new List<DbContextTransaction>();
#endif
            try
            {
                foreach (var context in dbContexts)
                {
                    list.Add(context.Database.BeginTransaction());

                    count += await context.SaveChangesAsync(cancellationToken);
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