using CodeArts.Db.Routes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 读写仓库（支持执行器）。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IDbRepository<TEntity> : IDRepository<TEntity>, IRepository<TEntity>, IOrderedQueryable<TEntity>, IQueryable<TEntity>, IEnumerable<TEntity>, IRepository, IOrderedQueryable, IQueryable, IEnumerable where TEntity : class, IEntiy
    {
        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <returns></returns>
        IInsertable<TEntity> AsInsertable(TEntity entry);

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        IInsertable<TEntity> AsInsertable(List<TEntity> entries);

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        IInsertable<TEntity> AsInsertable(TEntity[] entries);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <returns></returns>
        IUpdateable<TEntity> AsUpdateable(TEntity entry);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        IUpdateable<TEntity> AsUpdateable(List<TEntity> entries);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        IUpdateable<TEntity> AsUpdateable(TEntity[] entries);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <returns></returns>
        IDeleteable<TEntity> AsDeleteable(TEntity entry);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        IDeleteable<TEntity> AsDeleteable(List<TEntity> entries);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        IDeleteable<TEntity> AsDeleteable(TEntity[] entries);

    }
}
