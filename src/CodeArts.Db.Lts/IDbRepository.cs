using CodeArts.Db.Lts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
#if NET_NORMAL || NETSTANDARD2_0
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db
{
    /// <summary>
    /// 读写仓库。
    /// </summary>
    public interface IDbRepository<TEntity> : IRepository<TEntity>, IOrderedQueryable<TEntity>, IQueryable<TEntity>, IEnumerable<TEntity>, IOrderedQueryable, IQueryable, IEnumerable where TEntity : class, IEntiy
    {
        /// <summary>
        /// 数据来源。
        /// </summary>
        /// <param name="table">表。</param>
        /// <returns></returns>
        IDbRepository<TEntity> From(Func<ITableInfo, string> table);

        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        IDbRepository<TEntity> Where(Expression<Func<TEntity, bool>> expression);

        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。
        /// </summary>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        IDbRepository<TEntity> TimeOut(int commandTimeout);

        /// <summary>
        /// 更新数据。
        /// </summary>
        /// <param name="updateExp">更新的字段和值。</param>
        /// <returns></returns>
        int Update(Expression<Func<TEntity, TEntity>> updateExp);

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <returns></returns>
        int Delete();

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <param name="whereExp">条件表达式。</param>
        /// <returns></returns>
        int Delete(Expression<Func<TEntity, bool>> whereExp);

        /// <summary>
        /// 插入数据。
        /// </summary>
        /// <param name="querable">需要插入的数据。</param>
        /// <returns></returns>
        int Insert(IQueryable<TEntity> querable);

#if NET_NORMAL || NETSTANDARD2_0
        /// <summary>
        /// 更新数据。
        /// </summary>
        /// <param name="updateExp">更新的字段和值。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> UpdateAsync(Expression<Func<TEntity, TEntity>> updateExp, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> DeleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <param name="whereExp">条件表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExp, CancellationToken cancellationToken = default);

        /// <summary>
        /// 插入数据。
        /// </summary>
        /// <param name="querable">需要插入的数据。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> InsertAsync(IQueryable<TEntity> querable, CancellationToken cancellationToken = default);
#endif
    }
}
