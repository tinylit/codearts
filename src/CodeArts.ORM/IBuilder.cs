using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CodeArts.ORM
{
    /// <summary>
    /// CodeArts.ORM构造器
    /// </summary>
    public interface IBuilder : IDisposable
    {
        /// <summary>
        /// 表达式分析
        /// </summary>
        /// <param name="node">节点</param>
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

        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。<see cref="System.Data.IDbCommand.CommandTimeout"/>
        /// </summary>
        int? TimeOut { get; }
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
    /// <typeparam name="T">实体类型</typeparam>
    public interface IBuilder<T> : IBuilder
    {
        /// <summary>
        /// 执行行为
        /// </summary>
        ExecuteBehavior Behavior { get; }
    }
}
