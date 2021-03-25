using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if NET_NORMAL || NET_CORE
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据仓库。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
#if NET_NORMAL || NET_CORE
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
        /// <param name="context">数据上下文。</param>
        public DbRepository(IDbContext<TEntity> context) : base(context)
        {
        }

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <returns></returns>
        public IInsertable<TEntity> AsInsertable(TEntity entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return AsInsertable(new TEntity[] { entry });
        }

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public IInsertable<TEntity> AsInsertable(List<TEntity> entries) => DbWriter<TEntity>.AsInsertable(DbContext, entries);

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public IInsertable<TEntity> AsInsertable(TEntity[] entries) => DbWriter<TEntity>.AsInsertable(DbContext, entries);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <returns></returns>
        public IUpdateable<TEntity> AsUpdateable(TEntity entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return AsUpdateable(new TEntity[] { entry });
        }

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public IUpdateable<TEntity> AsUpdateable(List<TEntity> entries) => DbWriter<TEntity>.AsUpdateable(DbContext, entries);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public IUpdateable<TEntity> AsUpdateable(TEntity[] entries) => DbWriter<TEntity>.AsUpdateable(DbContext, entries);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <returns></returns>
        public IDeleteable<TEntity> AsDeleteable(TEntity entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return AsDeleteable(new TEntity[] { entry });
        }

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public IDeleteable<TEntity> AsDeleteable(List<TEntity> entries) => DbWriter<TEntity>.AsDeleteable(DbContext, entries);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public IDeleteable<TEntity> AsDeleteable(TEntity[] entries) => DbWriter<TEntity>.AsDeleteable(DbContext, entries);
    }
}
