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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
#if NET_NORMAL || NET_CORE
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// 仓库。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public class LinqRepository<TEntity> : ILinqRepository<TEntity>, ILinqRepository, IQueryable<TEntity>, IOrderedQueryable<TEntity>, IEnumerable<TEntity>, IQueryable, IOrderedQueryable, IEnumerable where TEntity : class, IEntiy
    {
        private readonly DbContext _dbContext;
        private readonly DbSet<TEntity> _dbSet;
        private readonly IQueryable<TEntity> _dbQueryable;

        /// <summary>
        /// inheritdoc
        /// </summary>
        public LinqRepository(DbContext context)
        {
            _dbContext = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<TEntity>();
            _dbQueryable = _dbSet.AsQueryable();
        }

        /// <summary>
        /// 该条目提供对更改跟踪信息和操作的访问。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <returns></returns>
#if NET_CORE
        public virtual EntityEntry<TEntity> Entry(TEntity entity) => _dbContext.Entry(entity);
#else
        public virtual DbEntityEntry<TEntity> Entry(TEntity entity) => _dbContext.Entry(entity);
#endif

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <param name="entity">实体。</param>
        public virtual void Delete(TEntity entity) => _dbSet.Remove(entity);

        /// <summary>
        /// 批量删除。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        public virtual void Delete(params TEntity[] entities) => _dbSet.RemoveRange(entities);

        /// <summary>
        /// 批量删除。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        public virtual void Delete(IEnumerable<TEntity> entities) => _dbSet.RemoveRange(entities);

        /// <summary>
        /// 插入数据。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <returns></returns>
#if NET_CORE
        public virtual EntityEntry<TEntity> Insert(TEntity entity) => _dbSet.Add(entity);
#else
        public virtual TEntity Insert(TEntity entity) => _dbSet.Add(entity);
#endif

        /// <summary>
        /// 批量插入。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        public virtual void Insert(params TEntity[] entities) => _dbSet.AddRange(entities);

        /// <summary>
        /// 批量插入。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        public virtual void Insert(IEnumerable<TEntity> entities) => _dbSet.AddRange(entities);

#if NET_CORE
        /// <summary>
        /// 插入数据。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public virtual ValueTask<EntityEntry<TEntity>> InsertAsync(TEntity entity, CancellationToken cancellationToken = default) => _dbSet.AddAsync(entity, cancellationToken);

        /// <summary>
        /// 批量插入。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <returns></returns>
        public virtual Task InsertAsync(params TEntity[] entities) => _dbSet.AddRangeAsync(entities);

        /// <summary>
        /// 批量插入。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public virtual Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => _dbSet.AddRangeAsync(entities, cancellationToken);

        /// <summary>
        /// 更新实体。
        /// </summary>
        /// <param name="entity">实体。</param>
        public virtual EntityEntry<TEntity> Update(TEntity entity) => _dbSet.Update(entity);

        private static string[] ExtractFields<TColumn>(Expression<Func<TEntity, TColumn>> lambda)
        {
            if (lambda is null)
            {
                throw new ArgumentNullException(nameof(lambda));
            }

            var parameter = lambda.Parameters[0];

            var body = lambda.Body;

            switch (body.NodeType)
            {
                case ExpressionType.Constant when body is ConstantExpression constant:
                    switch (constant.Value)
                    {
                        case string text:
                            return text.Split(',', ' ');
                        case string[] arr:
                            return arr;
                        default:
                            throw new NotImplementedException();
                    }
                case ExpressionType.MemberAccess when body is MemberExpression member:
                    return new string[] { member.Member.Name };
                case ExpressionType.MemberInit when body is MemberInitExpression memberInit:
                    return memberInit.Bindings.Select(x => x.Member.Name).ToArray();
                case ExpressionType.New when body is NewExpression newExpression:
                    return newExpression.Members.Select(x => x.Name).ToArray();
                case ExpressionType.Parameter:
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="updateFields">update fields.</param>
        public virtual void UpdateLimit<TColumn>(TEntity entity, Expression<Func<TEntity, TColumn>> updateFields)
        {
            var updates = ExtractFields(updateFields);

            var entry = _dbContext.Entry(entity);

            entry.OriginalValues.SetValues(entity);

            entry.State = EntityState.Unchanged;

            entry.Properties
                .Where(x => updates.Any(y => string.Equals(x.Metadata.Name, y, StringComparison.OrdinalIgnoreCase)))
                .ForEach(x =>
                {
                    x.IsModified = true;
                });
        }

        /// <summary>
        /// Updates the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="excludeFields">exclude update fields.</param>
        public virtual void UpdateExclude<TColumn>(TEntity entity, Expression<Func<TEntity, TColumn>> excludeFields)
        {
            var updates = ExtractFields(excludeFields);

            var entry = _dbContext.Entry(entity);

            entry.OriginalValues.SetValues(entity);

            entry.State = EntityState.Unchanged;

            foreach (var item in entry.Properties)
            {
                if (updates.Any(y => string.Equals(item.Metadata.Name, y, StringComparison.OrdinalIgnoreCase)) || item.Metadata.PropertyInfo.IsDefined(typeof(KeyAttribute), true))
                {
                    continue;
                }

                item.IsModified = true;
            }
        }

        /// <summary>
        /// 批量更新。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        public virtual void Update(params TEntity[] entities) => _dbSet.UpdateRange(entities);

        /// <summary>
        /// 批量更新。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        public virtual void Update(IEnumerable<TEntity> entities) => _dbSet.UpdateRange(entities);
#endif

        /// <summary>
        /// 数据库上下文。
        /// </summary>
        DbContext ILinqRepository.DBContext => _dbContext;

        /// <summary>
        /// inheritdoc
        /// </summary>
        Type IQueryable.ElementType => _dbQueryable.ElementType;

        /// <summary>
        /// inheritdoc
        /// </summary>
        Expression IQueryable.Expression => _dbQueryable.Expression;

        /// <summary>
        /// inheritdoc
        /// </summary>
        IQueryProvider IQueryable.Provider => _dbQueryable.Provider;

        /// <summary>
        /// inheritdoc
        /// </summary>
        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => _dbQueryable.GetEnumerator();

        /// <summary>
        /// inheritdoc
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => _dbQueryable.GetEnumerator();

#if NET_CORE
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
        public async Task<List<TEntity>> ToListAsync(CancellationToken cancellationToken = default)
        {
            var list = new List<TEntity>();

            var enumerator = _dbSet.AsAsyncEnumerable()
                .GetAsyncEnumerator(cancellationToken);

            while (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                list.Add(enumerator.Current);
            }

            return list;
        }

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
        public async Task<TEntity[]> ToArrayAsync(CancellationToken cancellationToken = default)
            => (await ToListAsync(cancellationToken).ConfigureAwait(false)).ToArray();

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
        public async Task ForEachAsync(Action<TEntity> action, CancellationToken cancellationToken = default)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var enumerator = _dbSet.AsAsyncEnumerable()
                .GetAsyncEnumerator(cancellationToken);

            while (await enumerator.MoveNextAsync())
            {
                action.Invoke(enumerator.Current);
            }
        }

        #endregion
#endif
    }

    /// <summary>
    /// 仓库。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    /// <typeparam name="TKey">主键类型。</typeparam>
    public class LinqRepository<TEntity, TKey> : LinqRepository<TEntity>, ILinqRepository<TEntity, TKey>, ILinqRepository<TEntity>, ILinqRepository, IQueryable<TEntity>, IOrderedQueryable<TEntity>, IEnumerable<TEntity>, IQueryable, IOrderedQueryable, IEnumerable
        where TEntity : class, IEntiy<TKey>, new()
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        /// <summary>
        /// inheritdoc
        /// </summary>
        public LinqRepository(DbContext dbContext) : base(dbContext)
        {
        }

        /// <summary>
        /// 根据主键删除数据。
        /// </summary>
        /// <param name="id">主键。</param>
        public virtual void Delete(TKey id)
        {
            var entry = new TEntity
            {
                Id = id
            };

            Entry(entry).State = EntityState.Deleted;
        }

        /// <summary>
        /// 是否存在数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <returns></returns>
        public virtual bool IsExists(TKey id) => this.Any(x => x.Id.Equals(id));

        /// <summary>
        /// 根据主键查找指定数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <returns></returns>
        public virtual TEntity First(TKey id) => this.First(x => x.Id.Equals(id));

        /// <summary>
        /// 根据主键查找指定数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <returns></returns>
        public virtual TEntity FirstOrDefault(TKey id) => this.FirstOrDefault(x => x.Id.Equals(id));

#if NET_NORMAL || NET_CORE
        /// <summary>
        /// 是否存在数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <returns></returns>
        public virtual Task<bool> IsExistsAsync(TKey id) => this.AnyAsync(x => x.Id.Equals(id));

        /// <summary>
        /// 根据主键异步查询指定数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public virtual Task<TEntity> FirstAsync(TKey id, CancellationToken cancellationToken = default) => this.FirstAsync(x => x.Id.Equals(id), cancellationToken);

        /// <summary>
        /// 根据主键异步查询指定数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public virtual Task<TEntity> FirstOrDefaultAsync(TKey id, CancellationToken cancellationToken = default) => this.FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);
#endif
    }
}
