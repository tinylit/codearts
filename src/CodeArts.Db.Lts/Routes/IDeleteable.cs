using System;
using System.Linq.Expressions;
#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.Lts.Routes
{
    /// <summary>
    /// 删除能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IDeleteable<TEntity> : IDeleteableByTransaction<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// SQL 监视。
        /// </summary>
        /// <param name="watchSql">监视器。</param>
        /// <returns></returns>
        IDeleteable<TEntity> WatchSql(Action<CommandSql> watchSql);

        /// <summary>
        /// 跳过幂等验证。
        /// </summary>
        /// <returns></returns>
        IDeleteable<TEntity> SkipIdempotentValid();

        /// <summary>
        /// 开启事务保护，使用数据库默认隔离级别。
        /// <see cref="System.Data.IDbConnection.BeginTransaction()"/>
        /// </summary>
        /// <returns></returns>
        IDeleteableByTransaction<TEntity> UseTransaction();

        /// <summary>
        /// 开启事务保护，设置事务隔离级别。
        /// <see cref="System.Data.IDbConnection.BeginTransaction(System.Data.IsolationLevel)"/>
        /// </summary>
        /// <param name="isolationLevel">隔离级别。</param>
        /// <returns></returns>
        IDeleteableByTransaction<TEntity> UseTransaction(System.Data.IsolationLevel isolationLevel);
    }

    /// <summary>
    /// 删除能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IDeleteableByTransaction<TEntity> : IDeleteableByFrom<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// 数据源。
        /// </summary>
        /// <param name="tableGetter">表名称。</param>
        /// <returns></returns>
        IDeleteableByFrom<TEntity> From(Func<ITableInfo, string> tableGetter);

        /// <summary>
        /// 数据源。
        /// </summary>
        /// <param name="tableGetter">表名称。</param>
        /// <returns></returns>
        IDeleteableByFrom<TEntity> From(Func<ITableInfo, TEntity, string> tableGetter);
    }

    /// <summary>
    /// 删除能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IDeleteableByFrom<TEntity> : IDeleteableByWhere<TEntity> where TEntity : class, IEntiy
    {
    }

    /// <summary>
    /// 删除能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IDeleteableByWhere<TEntity> : IDeleteableByCommit<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IDeleteableByWhere<TEntity> Where(string[] columns);

        /// <summary>
        /// 动作需要操作的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IDeleteableByWhere<TEntity> Where<TColumn>(Expression<Func<TEntity, TColumn>> columns);
    }

    /// <summary>
    /// 删除能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IDeleteableByCommit<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <param name="commandTimeout">超时时间，单位：秒。<see cref="System.Data.IDbCommand.CommandTimeout"/></param>
        /// <returns></returns>
        int ExecuteCommand(int? commandTimeout = null);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> ExecuteCommandAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <param name="commandTimeout">超时时间，单位：秒。<see cref="System.Data.IDbCommand.CommandTimeout"/></param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> ExecuteCommandAsync(int? commandTimeout, CancellationToken cancellationToken = default);
#endif
    }
}
