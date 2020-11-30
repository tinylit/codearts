using System;
using System.Linq.Expressions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 路由执行提供者。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IDbRouter<TEntity>  where TEntity : class, IEntiy
    {
        /// <summary>
        /// 动作需要操作的字段。
        /// </summary>
        /// <typeparam name="TColumn">获取字段的类型。</typeparam>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        Func<TEntity, string[]> Where<TColumn>(Expression<Func<TEntity, TColumn>> columns);

        /// <summary>
        /// 动作需要操作的字段。
        /// </summary>
        /// <typeparam name="TColumn">获取字段的类型。</typeparam>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        string[] Except<TColumn>(Expression<Func<TEntity, TColumn>> columns);

        /// <summary>
        /// 动作需要操作的字段。
        /// </summary>
        /// <typeparam name="TColumn">获取字段的类型。</typeparam>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        string[] Limit<TColumn>(Expression<Func<TEntity, TColumn>> columns);
    }
}
