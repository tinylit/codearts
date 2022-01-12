using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据库（开启事务后，创建的<see cref="IDbCommand"/>会自动设置事务。）。
    /// </summary>
    public partial interface IDatabase : IDisposable
    {
        /// <summary> 连接名称。 </summary>
        string Name { get; }

        /// <summary> 数据库驱动名称。 </summary>
        string ProviderName { get; }

        /// <summary>
        /// 数据库连接。
        /// </summary>
        [DebuggerHidden]
        string ConnectionString { get; }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        T Read<T>(Expression expression);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(Expression expression);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        int Execute(Expression expression);
    }
}
