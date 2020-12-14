using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 启动访问器。
    /// </summary>
    public interface IStartupVisitor : IVisitor, IDisposable
    {
        /// <summary>
        /// 启动。
        /// </summary>
        /// <param name="node">分析表达式。</param>
        /// <returns></returns>
        Expression Startup(Expression node);

        /// <summary>
        /// 参数。
        /// </summary>
        Dictionary<string, object> Parameters { get; }

        /// <summary>
        /// SQL语句。
        /// </summary>
        /// <returns></returns>
        string ToSQL();
    }
}
