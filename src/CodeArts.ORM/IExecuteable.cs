using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

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
    /// <typeparam name="T"></typeparam>
    public interface IRouteExecuteable<T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>
        /// 可编辑能力
        /// </summary>
        IEditable Editable { get; }

        /// <summary>
        /// 执行指令
        /// </summary>
        /// <returns></returns>
        int ExecuteCommand();

        /// <summary>
        /// 执行提供器
        /// </summary>
        IRouteExecuteProvider Provider { get; }
    }

    /// <summary>
    /// 删除能力
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IDeleteable<T> : IRouteExecuteable<T>
    {
        /// <summary>
        /// 数据源
        /// </summary>
        /// <param name="table">表名称</param>
        /// <returns></returns>
        IDeleteable<T> From(Func<ITableRegions, string> table);

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
    }

    /// <summary>
    /// 插入能力
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IInsertable<T> : IRouteExecuteable<T>
    {
        /// <summary>
        /// 数据源
        /// </summary>
        /// <param name="table">表名称</param>
        /// <returns></returns>
        IInsertable<T> From(Func<ITableRegions, string> table);

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
    }

    /// <summary>
    /// 更新能力
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IUpdateable<T> : IRouteExecuteable<T>
    {
        /// <summary>
        /// 数据源
        /// </summary>
        /// <param name="table">表名称</param>
        /// <returns></returns>
        IUpdateable<T> From(Func<ITableRegions, string> table);

        /// <summary>
        /// 数据源
        /// </summary>
        /// <param name="table">表名称</param>
        /// <returns></returns>
        IUpdateable<T> From(Func<ITableRegions, T, string> table);

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
    }
}
