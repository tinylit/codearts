using System;
using System.Linq.Expressions;
#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.Lts.Routes
{
    /// <summary>
    /// 更新能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IUpdateable<TEntity> : IUpdateableByFrom<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// 跳过幂等验证。
        /// </summary>
        /// <returns></returns>
        IUpdateable<TEntity> SkipIdempotentValid();

        /// <summary>
        /// 数据源。
        /// </summary>
        /// <param name="tableGetter">表名称。</param>
        /// <returns></returns>
        IUpdateableByFrom<TEntity> Table(Func<ITableInfo, string> tableGetter);

        /// <summary>
        /// 数据源。
        /// </summary>
        /// <param name="tableGetter">表名称。</param>
        /// <returns></returns>
        IUpdateableByFrom<TEntity> Table(Func<ITableInfo, TEntity, string> tableGetter);
    }

    /// <summary>
    /// 更新能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IUpdateableByFrom<TEntity> : IUpdateableByLimit<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// 只更新的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableByLimit<TEntity> Set(string[] columns);

        /// <summary>
        /// 只更新的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableByLimit<TEntity> Set<TColumn>(Expression<Func<TEntity, TColumn>> columns);

        /// <summary>
        /// 不更新的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableByLimit<TEntity> SetExcept(string[] columns);

        /// <summary>
        /// 不更新的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableByLimit<TEntity> SetExcept<TColumn>(Expression<Func<TEntity, TColumn>> columns);
    }

    /// <summary>
    /// 更新能力。
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IUpdateableByLimit<TEntity> : IUpdateableByWhere<TEntity> where TEntity : class, IEntiy
    {
    }

    /// <summary>
    /// 更新能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IUpdateableByWhere<TEntity> : IUpdateableByCommit<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableByWhere<TEntity> Where(string[] columns);

        /// <summary>
        /// 动作需要操作的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableByWhere<TEntity> Where<TColumn>(Expression<Func<TEntity, TColumn>> columns);
    }

    /// <summary>
    /// 更新能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IUpdateableByCommit<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// SQL 监视。
        /// </summary>
        /// <param name="watchSql">监视器。</param>
        /// <returns></returns>
        IUpdateableByCommit<TEntity> WatchSql(Action<CommandSql> watchSql);

        /// <summary>
        /// 开启事务保护，使用数据库默认隔离级别。
        /// <see cref="System.Data.IDbConnection.BeginTransaction()"/>
        /// </summary>
        /// <returns></returns>
        IUpdateableByCommit<TEntity> Transaction();

        /// <summary>
        /// 开启事务保护，设置事务隔离级别。
        /// <see cref="System.Data.IDbConnection.BeginTransaction(System.Data.IsolationLevel)"/>
        /// </summary>
        /// <param name="isolationLevel">隔离级别。</param>
        /// <returns></returns>
        IUpdateableByCommit<TEntity> Transaction(System.Data.IsolationLevel isolationLevel);

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
