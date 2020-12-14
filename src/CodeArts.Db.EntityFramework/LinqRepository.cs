#if NETSTANDARD2_0
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
#else
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#endif
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
#if !NET40
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db
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
#if NETSTANDARD2_0
        public EntityEntry<TEntity> Entry(TEntity entity) => _dbContext.Entry(entity);
#else
        public DbEntityEntry<TEntity> Entry(TEntity entity) => _dbContext.Entry(entity);
#endif

        /// <summary>
        /// 统计符合条件的总行数。
        /// </summary>
        /// <param name="predicate">条件。</param>
        /// <returns></returns>
        public int Count(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate is null)
            {
                return _dbSet.Count();
            }
            return _dbSet.Count(predicate);
        }

#if !NET40
        /// <summary>
        /// 统计符合条件的总行数。
        /// </summary>
        /// <param name="predicate">条件。</param>
        /// <returns></returns>
        public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate is null)
            {
                return _dbSet.CountAsync();
            }

            return _dbSet.CountAsync(predicate);
        }
#endif

        /// <summary>
        /// 统计符合条件的总行数。
        /// </summary>
        /// <param name="predicate">条件。</param>
        /// <returns></returns>
        public long LongCount(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate is null)
            {
                return _dbSet.LongCount();
            }

            return _dbSet.LongCount(predicate);
        }

#if !NET40
        /// <summary>
        /// 统计符合条件的总行数。
        /// </summary>
        /// <param name="predicate">条件。</param>
        /// <returns></returns>
        public Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate is null)
            {
                return _dbSet.LongCountAsync();
            }

            return _dbSet.LongCountAsync(predicate);
        }
#endif

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <param name="entity">实体。</param>
        public void Delete(TEntity entity) => _dbSet.Remove(entity);

        /// <summary>
        /// 批量删除。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        public void Delete(params TEntity[] entities) => _dbSet.RemoveRange(entities);

        /// <summary>
        /// 批量删除。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        public void Delete(IEnumerable<TEntity> entities) => _dbSet.RemoveRange(entities);

        /// <summary>
        /// 是否包含符合条件的数据。
        /// </summary>
        /// <param name="predicate">条件。</param>
        /// <returns></returns>
        public bool Exists(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate is null)
            {
                return _dbSet.Any();
            }

            return _dbSet.Any(predicate);
        }

#if !NET40
        /// <summary>
        /// 是否包含符合条件的数据。
        /// </summary>
        /// <param name="predicate">条件。</param>
        /// <returns></returns>
        public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate is null)
            {
                return _dbSet.AnyAsync();
            }

            return _dbSet.AnyAsync(predicate);
        }
#endif

        /// <summary>
        /// 插入数据。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <returns></returns>
#if NETSTANDARD2_0
        public EntityEntry<TEntity> Insert(TEntity entity) => _dbSet.Add(entity);
#else
        public TEntity Insert(TEntity entity) => _dbSet.Add(entity);
#endif

        /// <summary>
        /// 批量插入。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        public void Insert(params TEntity[] entities) => _dbSet.AddRange(entities);

        /// <summary>
        /// 批量插入。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        public void Insert(IEnumerable<TEntity> entities) => _dbSet.AddRange(entities);

#if NETSTANDARD2_0
        /// <summary>
        /// 插入数据。
        /// </summary>
        /// <param name="entity">实体。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public ValueTask<EntityEntry<TEntity>> InsertAsync(TEntity entity, CancellationToken cancellationToken = default) => _dbSet.AddAsync(entity, cancellationToken);

        /// <summary>
        /// 批量插入。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <returns></returns>
        public Task InsertAsync(params TEntity[] entities) => _dbSet.AddRangeAsync(entities);

        /// <summary>
        /// 批量插入。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) => _dbSet.AddRangeAsync(entities, cancellationToken);

        /// <summary>
        /// 更新实体。
        /// </summary>
        /// <param name="entity">实体。</param>
        public EntityEntry<TEntity> Update(TEntity entity) => _dbSet.Update(entity);

        /// <summary>
        /// 批量更新。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        public void Update(params TEntity[] entities) => _dbSet.UpdateRange(entities);

        /// <summary>
        /// 批量更新。
        /// </summary>
        /// <param name="entities">实体集合。</param>
        public void Update(IEnumerable<TEntity> entities) => _dbSet.UpdateRange(entities);
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
        public void Delete(TKey id)
        {
            var entry = new TEntity
            {
                Id = id
            };

            Entry(entry).State = EntityState.Deleted;
        }

        /// <summary>
        /// 根据主键查找指定数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <returns></returns>
        public TEntity Find(TKey id) => this.FirstOrDefault(x => x.Id.Equals(id));

#if !NET40
        /// <summary>
        /// 根据主键异步查询指定数据。
        /// </summary>
        /// <param name="id">主键。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<TEntity> FindAsync(TKey id, CancellationToken cancellationToken = default) => this.FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);
#endif
    }
}
