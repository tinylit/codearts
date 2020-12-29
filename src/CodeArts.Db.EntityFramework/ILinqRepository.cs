#if NET_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
#else
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db
{
    /// <summary>
    /// 仓库。
    /// </summary>
    public interface ILinqRepository
    {
        /// <summary>
        /// 数据库上下文。
        /// </summary>
        DbContext DBContext { get; }
    }

    /// <summary>
    /// 仓库。
    /// </summary>
    public interface ILinqRepository<TEntity> : IQueryable<TEntity>, IEnumerable<TEntity>, IQueryable, IEnumerable
        where TEntity : class, IEntiy
    {
        /// <summary>
        ///  Gets an Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry`1 for the given
        ///  entity. The entry provides access to change tracking information and operations
        ///  for the entity.
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <returns></returns>
#if NET_CORE
        EntityEntry<TEntity> Entry(TEntity entity);
#else
        DbEntityEntry<TEntity> Entry(TEntity entity);
#endif

        /// <summary>
        /// Inserts a new entity synchronously.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
#if NET_CORE
        EntityEntry<TEntity> Insert(TEntity entity);
#else
        TEntity Insert(TEntity entity);
#endif

        /// <summary>
        /// Inserts a range of entities synchronously.
        /// </summary>
        /// <param name="entities">The entities to insert.</param>
        void Insert(params TEntity[] entities);

        /// <summary>
        /// Inserts a range of entities synchronously.
        /// </summary>
        /// <param name="entities">The entities to insert.</param>
        void Insert(IEnumerable<TEntity> entities);

#if NET_CORE
        /// <summary>
        /// Inserts a new entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous insert operation.</returns>

        ValueTask<EntityEntry<TEntity>> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts a range of entities asynchronously.
        /// </summary>
        /// <param name="entities">The entities to insert.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous insert operation.</returns>
        Task InsertAsync(params TEntity[] entities);

        /// <summary>
        /// Inserts a range of entities asynchronously.
        /// </summary>
        /// <param name="entities">The entities to insert.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous insert operation.</returns>
        Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        EntityEntry<TEntity> Update(TEntity entity);

        /// <summary>
        /// Updates the specified entities.
        /// </summary>
        /// <param name="entities">The entities.</param>
        void Update(params TEntity[] entities);

        /// <summary>
        /// Updates the specified entities.
        /// </summary>
        /// <param name="entities">The entities.</param>
        void Update(IEnumerable<TEntity> entities);
#endif

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        void Delete(TEntity entity);

        /// <summary>
        /// Deletes the specified entities.
        /// </summary>
        /// <param name="entities">The entities.</param>
        void Delete(params TEntity[] entities);

        /// <summary>
        /// Deletes the specified entities.
        /// </summary>
        /// <param name="entities">The entities.</param>
        void Delete(IEnumerable<TEntity> entities);

#if NETSTANDARD2_0
        #region ForEach
        #region ToList/Array

        /// <summary>
        ///     Asynchronously creates a <see cref="List{T}" /> from an <see cref="IQueryable{T}" /> by enumerating it
        ///     asynchronously.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use <see langword="await" /> to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="cancellationToken"> A <see cref="CancellationToken" /> to observe while waiting for the task to complete. </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains a <see cref="List{T}" /> that contains elements from the input sequence.
        /// </returns>
        /// <exception cref="OperationCanceledException"> If the <see cref="CancellationToken"/> is canceled. </exception>
        Task<List<TEntity>> ToListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Asynchronously creates an array from an <see cref="IQueryable{T}" /> by enumerating it asynchronously.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use <see langword="await" /> to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="cancellationToken"> A <see cref="CancellationToken" /> to observe while waiting for the task to complete. </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains an array that contains elements from the input sequence.
        /// </returns>
        /// <exception cref="OperationCanceledException"> If the <see cref="CancellationToken"/> is canceled. </exception>
        Task<TEntity[]> ToArrayAsync(CancellationToken cancellationToken = default);

        #endregion

        /// <summary>
        ///     Asynchronously enumerates the query results and performs the specified action on each element.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use <see langword="await" /> to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="action"> The action to perform on each element. </param>
        /// <param name="cancellationToken"> A <see cref="CancellationToken" /> to observe while waiting for the task to complete. </param>
        /// <returns> A task that represents the asynchronous operation. </returns>
        /// <exception cref="OperationCanceledException"> If the <see cref="CancellationToken"/> is canceled. </exception>
        Task ForEachAsync(Action<TEntity> action, CancellationToken cancellationToken = default);

        #endregion
#endif
    }

    /// <summary>
    /// 仓库。
    /// </summary>
    public interface ILinqRepository<TEntity, TKey> : ILinqRepository<TEntity>, IQueryable<TEntity>, IEnumerable<TEntity>, IQueryable, IEnumerable
        where TEntity : class, IEntiy<TKey>
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        /// <summary>
        /// 是否存在数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <returns></returns>
        bool IsExists(TKey id);

        /// <summary>
        /// 查找第一个数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <returns></returns>
        TEntity First(TKey id);

        /// <summary>
        /// 查找第一个数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <returns></returns>
        TEntity FirstOrDefault(TKey id);

#if NET_NORMAL || NET_CORE
        /// <summary>
        /// 是否存在数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <returns></returns>
        Task<bool> IsExistsAsync(TKey id);

        /// <summary>
        /// 查找第一个数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<TEntity> FirstAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查找第一个数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<TEntity> FirstOrDefaultAsync(TKey id, CancellationToken cancellationToken = default);
#endif

        /// <summary>
        /// Deletes the entity by the specified primary key.
        /// </summary>
        /// <param name="id">The primary key value.</param>
        void Delete(TKey id);
    }
}
