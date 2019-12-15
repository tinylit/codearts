using System;
using System.Linq.Expressions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 路由执行提供者
    /// </summary>
    public interface IRouteExecuteProvider
    {
        /// <summary>
        /// 动作需要操作的字段
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TColumn">获取字段的类型</typeparam>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        Func<T, string[]> Where<T, TColumn>(Expression<Func<T, TColumn>> columns);

        /// <summary>
        /// 动作需要操作的字段
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TColumn">获取字段的类型</typeparam>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        string[] Except<T, TColumn>(Expression<Func<T, TColumn>> columns);

        /// <summary>
        /// 动作需要操作的字段
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <typeparam name="TColumn">获取字段的类型</typeparam>
        /// <param name="columns">字段</param>
        /// <returns></returns>
        string[] Limit<T, TColumn>(Expression<Func<T, TColumn>> columns);
    }
}
