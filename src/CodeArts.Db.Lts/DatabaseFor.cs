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
    public abstract class DatabaseFor : IDatabaseFor
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
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        public abstract int Execute(IDbConnection connection, CommandSql commandSql);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public abstract Task<int> ExecuteAsync(IDbConnection connection, CommandSql commandSql, CancellationToken cancellationToken);
#endif

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        public abstract IEnumerable<T> Query<T>(IDbConnection connection, CommandSql commandSql);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        public abstract IAsyncEnumerable<T> QueryAsync<T>(IDbConnection connection, CommandSql commandSql);
#endif

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        public abstract T Read<T>(IDbConnection connection, CommandSql<T> commandSql);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public abstract Task<T> ReadAsync<T>(IDbConnection connection, CommandSql<T> commandSql, CancellationToken cancellationToken);
#endif

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
        public virtual CommandSql<T> Read<T>(Expression expression)
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
        public virtual CommandSql Execute(Expression expression)
        {
            using (var visitor = CreateExe())
            {
                visitor.Startup(expression);

                return visitor.ToSQL();
            }
        }
    }
}
