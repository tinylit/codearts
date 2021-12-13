using System;
using System.Linq.Expressions;
#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.Routes
{
    /// <summary>
    /// 插入能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IInsertable<TEntity> : IInsertableByFrom<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// 数据源。
        /// </summary>
        /// <param name="tableGetter">表名称。</param>
        /// <returns></returns>
        IInsertableByFrom<TEntity> Into(Func<ITableInfo, string> tableGetter);

        /// <summary>
        /// 数据源。
        /// </summary>
        /// <param name="tableGetter">表名称。</param>
        /// <returns></returns>
        IInsertableByFrom<TEntity> Into(Func<ITableInfo, TEntity, string> tableGetter);
    }

    /// <summary>
    /// 插入能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IInsertableByFrom<TEntity> : IInsertableByCommit<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// 只插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IInsertableByCommit<TEntity> Limit(string[] columns);

        /// <summary>
        /// 只插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IInsertableByCommit<TEntity> Limit<TColumn>(Expression<Func<TEntity, TColumn>> columns);

        /// <summary>
        /// 不插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IInsertableByCommit<TEntity> Except(string[] columns);

        /// <summary>
        /// 不插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IInsertableByCommit<TEntity> Except<TColumn>(Expression<Func<TEntity, TColumn>> columns);
    }

    /// <summary>
    /// 插入能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IInsertableByCommit<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// SQL 监视。
        /// </summary>
        /// <param name="watchSql">监视器。</param>
        /// <returns></returns>
        IInsertableByCommit<TEntity> WatchSql(Action<CommandSql> watchSql);

        /// <summary>
        /// 开启事务保护，使用数据库默认隔离级别。
        /// <see cref="System.Data.IDbConnection.BeginTransaction()"/>
        /// </summary>
        /// <returns></returns>
        IInsertableByCommit<TEntity> Transaction();

        /// <summary>
        /// 开启事务保护，设置事务隔离级别。
        /// <see cref="System.Data.IDbConnection.BeginTransaction(System.Data.IsolationLevel)"/>
        /// </summary>
        /// <param name="isolationLevel">隔离级别。</param>
        /// <returns></returns>
        IInsertableByCommit<TEntity> Transaction(System.Data.IsolationLevel isolationLevel);

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
