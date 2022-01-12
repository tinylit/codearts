using CodeArts.Db.Exceptions;
using CodeArts.Db.Expressions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据库。
    /// </summary>
    public class DatabaseFor
    {
        private readonly ISQLCorrectSettings settings;
        private readonly ICustomVisitorList visitors;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="settings">SQL语句矫正设置。</param>
        /// <param name="visitors">自定义访问器。</param>
        public DatabaseFor(ISQLCorrectSettings settings, ICustomVisitorList visitors)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.visitors = visitors ?? throw new ArgumentNullException(nameof(visitors));
        }

        /// <summary>
        /// 查询访问器。
        /// </summary>
        /// <returns></returns>
        protected virtual IQueryVisitor Create() => new QueryVisitor(settings, visitors);

        /// <summary>
        /// 执行访问器。
        /// </summary>
        /// <returns></returns>
        protected virtual IExecuteVisitor CreateExe() => new ExecuteVisitor(settings, visitors);

        /// <summary>
        /// 读取命令。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        protected virtual CommandSql<T> Read<T>(Expression expression)
        {
            using (var visitor = Create())
            {
                visitor.Startup(expression);

                return visitor.ToSQL<T>();
            }
        }

        /// <summary>
        /// 写入命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        protected virtual CommandSql Execute(Expression expression)
        {
            using (var visitor = CreateExe())
            {
                visitor.Startup(expression);

                return visitor.ToSQL();
            }
        }
    }
}
