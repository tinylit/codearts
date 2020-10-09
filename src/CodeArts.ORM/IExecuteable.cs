using System;
using System.Linq.Expressions;
using System.Transactions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 执行力
    /// </summary>
    public interface IExecuteable
    {
        /// <summary>
        /// 表达式
        /// </summary>
        Expression Expression { get; }
    }

    /// <summary>
    /// 执行力
    /// </summary>
    public interface IExecuteable<T> : IExecuteable
    {
        /// <summary>
        /// 执行提供器
        /// </summary>
        IExecuteProvider<T> Provider { get; }
    }

    /// <summary>
    /// 路由执行力
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface IRouteExecuteable<T>
    {
        /// <summary>
        /// 矫正配置。
        /// </summary>
        ISQLCorrectSimSettings Settings { get; }

        /// <summary>
        /// 执行指令
        /// </summary>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns></returns>
        int ExecuteCommand(int? commandTimeout = null);

        /// <summary>
        /// 执行提供器
        /// </summary>
        IRouteExecuteProvider<T> Provider { get; }
    }

    /// <summary>
    /// 删除能力
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface IDeleteable<T> : IRouteExecuteable<T>
    {
        /// <summary>
        /// 数据源
        /// </summary>
        /// <param name="table">表名称</param>
        /// <returns></returns>
        IDeleteable<T> From(Func<ITableInfo, string> table);

        /// <summary>
        /// 条件
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        IDeleteable<T> Where(string[] columns);

        /// <summary>
        /// 动作需要操作的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        IDeleteable<T> Where<TColumn>(Expression<Func<T, TColumn>> columns);

        /// <summary>
        /// 设置事务，默认:<see cref="TransactionScopeOption.Required"/>。
        /// </summary>
        /// <param name="option">配置</param>
        /// <returns></returns>
        IDeleteable<T> UseTransaction(TransactionScopeOption option);
    }

    /// <summary>
    /// 插入能力
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface IInsertable<T> : IRouteExecuteable<T>
    {
        /// <summary>
        /// 数据源
        /// </summary>
        /// <param name="table">表名称</param>
        /// <returns></returns>
        IInsertable<T> From(Func<ITableInfo, string> table);

        /// <summary>
        /// 只更新或只插入的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        IInsertable<T> Limit(string[] columns);

        /// <summary>
        /// 只更新或只插入的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        IInsertable<T> Limit<TColumn>(Expression<Func<T, TColumn>> columns);

        /// <summary>
        /// 不更新或不插入的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        IInsertable<T> Except(string[] columns);

        /// <summary>
        /// 不更新或不插入的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        IInsertable<T> Except<TColumn>(Expression<Func<T, TColumn>> columns);

        /// <summary>
        /// 设置事务，默认:<see cref="TransactionScopeOption.Required"/>。
        /// </summary>
        /// <param name="option">配置</param>
        /// <returns></returns>
        IInsertable<T> UseTransaction(TransactionScopeOption option);
    }

    /// <summary>
    /// 更新能力
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface IUpdateable<T> : IRouteExecuteable<T>
    {
        /// <summary>
        /// 数据源
        /// </summary>
        /// <param name="table">表名称</param>
        /// <returns></returns>
        IUpdateable<T> From(Func<ITableInfo, string> table);

        /// <summary>
        /// 数据源
        /// </summary>
        /// <param name="table">表名称</param>
        /// <returns></returns>
        IUpdateable<T> From(Func<ITableInfo, T, string> table);

        /// <summary>
        /// 只更新或只插入的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        IUpdateable<T> Limit(string[] columns);

        /// <summary>
        /// 只更新或只插入的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        IUpdateable<T> Limit<TColumn>(Expression<Func<T, TColumn>> columns);

        /// <summary>
        /// 不更新或不插入的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        IUpdateable<T> Except(string[] columns);

        /// <summary>
        /// 不更新或不插入的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        IUpdateable<T> Except<TColumn>(Expression<Func<T, TColumn>> columns);

        /// <summary>
        /// 条件
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        IUpdateable<T> Where(string[] columns);

        /// <summary>
        /// 动作需要操作的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        IUpdateable<T> Where<TColumn>(Expression<Func<T, TColumn>> columns);

        /// <summary>
        /// 使用事务。
        /// </summary>
        /// <param name="option">配置</param>
        /// <returns></returns>
        IUpdateable<T> UseTransaction(TransactionScopeOption option);
    }
}
