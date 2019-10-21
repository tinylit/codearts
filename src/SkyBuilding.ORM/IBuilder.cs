using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// SkyBuilding.ORM构造器
    /// </summary>
    public interface IBuilder : IDisposable
    {
        /// <summary>
        /// 表达式分析
        /// </summary>
        /// <param name="node"></param>
        void Evaluate(Expression node);

        /// <summary>
        /// 参数
        /// </summary>
        Dictionary<string, object> Parameters { get; }

        /// <summary>
        /// SQL语句
        /// </summary>
        /// <returns></returns>
        string ToSQL();
    }

    /// <summary>
    /// 查询器
    /// </summary>
    public interface IQueryBuilder : IBuilder
    {
        /// <summary>
        /// 是否必须有查询或执行结果
        /// </summary>
        bool Required { get; }

        /// <summary>
        /// 默认值
        /// </summary>
        object DefaultValue { get; }
    }

    /// <summary>
    /// 增删改
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IBuilder<T> : IBuilder
    {
        /// <summary>
        /// 执行行为
        /// </summary>
        ExecuteBehavior Behavior { get; }
    }
}
