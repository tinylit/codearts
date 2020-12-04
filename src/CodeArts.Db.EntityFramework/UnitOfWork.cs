#if NETSTANDARD2_0
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
#else
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#endif
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// Represents the default implementation of the <see cref="IUnitOfWork"/> interface.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private bool disposed = false;
        private readonly DbContext[] dbContexts;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork{TContext}"/> class.
        /// </summary>
        /// <param name="repositories">The contexts.</param>
        public UnitOfWork(params IRepository[] repositories)
        {
            if (repositories is null)
            {
                throw new ArgumentNullException(nameof(repositories));
            }

            this.dbContexts = repositories.Select(x => x.DBContext).ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork{TContext}"/> class.
        /// </summary>
        /// <param name="dbContexts">The contexts.</param>
        public UnitOfWork(params DbContext[] dbContexts)
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
            catch (Exception exc)
            {
                foreach (var item in list)
                {
                    item.Rollback();
                }

                throw exc;
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
            catch (Exception exc)
            {
                foreach (var item in list)
                {
#if NETSTANDARD2_0
                    await item.RollbackAsync(cancellationToken);
#else
                    item.Rollback();
#endif
                }

                throw exc;
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
            catch (Exception exc)
            {
                foreach (var item in list)
                {
                    item.Rollback();
                }

                throw exc;
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

    /// <summary>
    /// Represents the default implementation of the <see cref="IUnitOfWork"/> and <see cref="IUnitOfWork{TContext}"/> interface.
    /// </summary>
    /// <typeparam name="TContext">The type of the db context.</typeparam>
    public class UnitOfWork<TContext> : IUnitOfWork<TContext>, IUnitOfWork where TContext : DbContext
    {
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork{TContext}"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public UnitOfWork(TContext context)
        {
            DbContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gets the db context.
        /// </summary>
        /// <returns>The instance of type <typeparamref name="TContext"/>.</returns>
        public TContext DbContext { get; }

        /// <summary>
        /// Changes the database name. This require the databases in the same machine. NOTE: This only work for MySQL right now.
        /// </summary>
        /// <param name="database">The database name.</param>
        /// <remarks>
        /// This only been used for supporting multiple databases in the same model. This require the databases in the same machine.
        /// </remarks>
        public void ChangeDatabase(string database)
        {
#if NETSTANDARD2_0
            var connection = DbContext.Database.GetDbConnection();
#else
            var connection = DbContext.Database.Connection;
#endif
            if (connection.State.HasFlag(ConnectionState.Open))
            {
                connection.ChangeDatabase(database);
            }
            else
            {
                var connectionString = Regex.Replace(connection.ConnectionString.Replace(" ", ""), @"(?<=[Dd]atabase=)\w+(?=;)", database, RegexOptions.Singleline);
                connection.ConnectionString = connectionString;
            }

#if NETSTANDARD2_0
            // Following code only working for mysql.
            var items = DbContext.Model.GetEntityTypes();
            foreach (var item in items)
            {
                if (item is IConventionEntityType entityType)
                {
                    entityType.SetSchema(database);
                }
            }
#endif
        }

        /// <summary>
        /// Executes the specified raw SQL command.
        /// </summary>
        /// <param name="sql">The raw SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The number of state entities written to database.</returns>
        public int ExecuteSqlCommand(string sql, params object[] parameters)
#if NETSTANDARD2_0
            => DbContext.Database.ExecuteSqlRaw(sql, parameters);
#else
            => DbContext.Database.ExecuteSqlCommand(sql, parameters);
#endif

#if NETSTANDARD2_0
        /// <summary>
        /// Uses raw SQL queries to fetch the specified <typeparamref name="TEntity" /> data.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="sql">The raw SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>An <see cref="IQueryable{T}" /> that contains elements that satisfy the condition specified by raw SQL.</returns>
        public IQueryable<TEntity> FromSql<TEntity>(string sql, params object[] parameters) where TEntity : class

            => DbContext.Set<TEntity>().FromSqlRaw(sql, parameters);
#else
        /// <summary>
        /// Uses raw SQL queries to fetch the specified <typeparamref name="TEntity" /> data.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="sql">The raw SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>An <see cref="DbRawSqlQuery{T}" /> that contains elements that satisfy the condition specified by raw SQL.</returns>
        public DbRawSqlQuery<TEntity> FromSql<TEntity>(string sql, params object[] parameters) where TEntity : class
            => DbContext.Database.SqlQuery<TEntity>(sql, parameters);
#endif

#if NETSTANDARD2_0
        /// <summary>
        /// 轨迹。
        /// </summary>
        /// <param name="rootEntity">实体。</param>
        /// <param name="callback">回调。</param>
        public void TrackGraph(object rootEntity, Action<EntityEntryGraphNode> callback)
        {
            DbContext.ChangeTracker.TrackGraph(rootEntity, callback);
        }

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges is called after the changes have been sent successfully to the database.</param>
        /// <returns>The number of state entries written to the database.</returns>
        public int Commit(bool acceptAllChangesOnSuccess = false) => DbContext.SaveChanges(acceptAllChangesOnSuccess);
#else
        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        public int Commit() => DbContext.SaveChanges();
#endif

#if !NET40
        /// <summary>
        /// Saves all changes made in this context to the database with distributed transaction.
        /// </summary>        
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous save operation. The task result contains the number of state entities written to database.</returns>
        public async Task<int> CommitAsync(CancellationToken cancellationToken = default) => await DbContext.SaveChangesAsync(cancellationToken);
#endif

#if NETSTANDARD2_0
        /// <summary>
        /// Saves all changes made in this context to the database with distributed transaction.
        /// </summary>        
        /// <param name="acceptAllChangesOnSuccess">Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges is called after the changes have been sent successfully to the database.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous save operation. The task result contains the number of state entities written to database.</returns>
        public async Task<int> CommitAsync(bool acceptAllChangesOnSuccess = false, CancellationToken cancellationToken = default) => await DbContext.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
#endif

#if !NET40
        /// <summary>
        /// Saves all changes made in this context to the database with distributed transaction.
        /// </summary>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <param name="unitOfWorks">An optional <see cref="IUnitOfWork"/> array.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous save operation. The task result contains the number of state entities written to database.</returns>
        public async Task<int> CommitAsync(CancellationToken cancellationToken = default, params IUnitOfWork[] unitOfWorks)
        {
            using (var ts = new TransactionScope())
            {
                var count = 0;

                foreach (var unitOfWork in unitOfWorks)
                {
                    count += await unitOfWork.CommitAsync(cancellationToken);
                }

                count += await CommitAsync(cancellationToken);

                ts.Complete();

                return count;
            }
        }
#endif

#if NETSTANDARD2_0
        /// <summary>
        /// Saves all changes made in this context to the database with distributed transaction.
        /// </summary>        
        /// <param name="acceptAllChangesOnSuccess">Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges is called after the changes have been sent successfully to the database.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <param name="unitOfWorks">An optional <see cref="IUnitOfWork"/> array.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous save operation. The task result contains the number of state entities written to database.</returns>
        public async Task<int> CommitAsync(bool acceptAllChangesOnSuccess = false, CancellationToken cancellationToken = default, params IUnitOfWork[] unitOfWorks)
        {
            using (var ts = new TransactionScope())
            {
                var count = 0;

                foreach (var unitOfWork in unitOfWorks)
                {
                    count += await unitOfWork.CommitAsync(acceptAllChangesOnSuccess, cancellationToken);
                }

                count += await CommitAsync(acceptAllChangesOnSuccess, cancellationToken);

                ts.Complete();

                return count;
            }
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
                    disposed = true;
                    // dispose the db context.
                    DbContext.Dispose();
                }
            }
        }
    }
}