using CodeArts.Db.Routes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据仓库。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
    public class DbRepository<TEntity> : DRepository<TEntity>, IDbRepository<TEntity>, IDRepository<TEntity>, IRepository<TEntity>, IOrderedQueryable<TEntity>, IQueryable<TEntity>, IAsyncEnumerable<TEntity>, IEnumerable<TEntity>, IRepository, IAsyncQueryProvider, IQueryProvider, IOrderedQueryable, IQueryable, IEnumerable where TEntity : class, IEntiy
#else
    public class DbRepository<TEntity> : DRepository<TEntity>, IDbRepository<TEntity>, IDRepository<TEntity>, IRepository<TEntity>, IOrderedQueryable<TEntity>, IQueryable<TEntity>, IEnumerable<TEntity>, IRepository, IQueryProvider, IOrderedQueryable, IQueryable, IEnumerable where TEntity : class, IEntiy
#endif
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DbRepository() : base() { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="connectionConfig">链接配置。</param>
        public DbRepository(IReadOnlyConnectionConfig connectionConfig) : base(connectionConfig) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="database">数据库。</param>
        public DbRepository(IDatabase database) : base(database)
        {
        }

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <returns></returns>
        public virtual IInsertable<TEntity> AsInsertable(TEntity entry)
            => new DbServiceSet<TEntity>(DbAdapter, ConnectionString).AsInsertable(entry);

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public virtual IInsertable<TEntity> AsInsertable(List<TEntity> entries)
            => new DbServiceSet<TEntity>(DbAdapter, ConnectionString).AsInsertable(entries);

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public virtual IInsertable<TEntity> AsInsertable(TEntity[] entries)
            => new DbServiceSet<TEntity>(DbAdapter, ConnectionString).AsInsertable(entries);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <returns></returns>
        public virtual IUpdateable<TEntity> AsUpdateable(TEntity entry)
            => new DbServiceSet<TEntity>(DbAdapter, ConnectionString).AsUpdateable(entry);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public virtual IUpdateable<TEntity> AsUpdateable(List<TEntity> entries)
            => new DbServiceSet<TEntity>(DbAdapter, ConnectionString).AsUpdateable(entries);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public virtual IUpdateable<TEntity> AsUpdateable(TEntity[] entries)
            => new DbServiceSet<TEntity>(DbAdapter, ConnectionString).AsUpdateable(entries);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <returns></returns>
        public virtual IDeleteable<TEntity> AsDeleteable(TEntity entry)
            => new DbServiceSet<TEntity>(DbAdapter, ConnectionString).AsDeleteable(entry);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public virtual IDeleteable<TEntity> AsDeleteable(List<TEntity> entries)
            => new DbServiceSet<TEntity>(DbAdapter, ConnectionString).AsDeleteable(entries);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public virtual IDeleteable<TEntity> AsDeleteable(TEntity[] entries)
            => new DbServiceSet<TEntity>(DbAdapter, ConnectionString).AsDeleteable(entries);
    }
}
