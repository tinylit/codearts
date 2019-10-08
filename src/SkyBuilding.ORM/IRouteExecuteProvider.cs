using System;
using System.Linq.Expressions;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 路由执行提供者
    /// </summary>
    /// <typeparam name="T">项</typeparam>
    public interface IRouteExecuteProvider
    {
        /// <summary>
        /// 动作需要操作的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        Func<T, string[]> Where<T, TColumn>(Expression<Func<T, TColumn>> columns);

        /// <summary>
        /// 动作需要操作的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        string[] Except<T, TColumn>(Expression<Func<T, TColumn>> columns);

        /// <summary>
        /// 动作需要操作的字段
        /// </summary>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        string[] Limit<T, TColumn>(Expression<Func<T, TColumn>> columns);
    }
}
