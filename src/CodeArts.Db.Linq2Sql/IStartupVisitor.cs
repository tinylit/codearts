using System;
using System.Linq.Expressions;

namespace CodeArts.Db
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
        void Startup(Expression node);
    }
}
