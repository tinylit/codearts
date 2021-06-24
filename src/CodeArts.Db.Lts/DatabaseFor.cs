using CodeArts.Db.Exceptions;
using CodeArts.Db.Lts.Visitors;
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
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public int Execute(IDbConnection connection, Expression expression)
        {
            var commandSql = CreateWriteCommandSql(expression);

            SqlCapture.Current?.Capture(commandSql);

            return Execute(connection, commandSql);

        }

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        protected abstract int Execute(IDbConnection connection, CommandSql commandSql);

        #if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<int> ExecuteAsync(IDbConnection connection, Expression expression, CancellationToken cancellationToken = default)
        {
            var commandSql = CreateWriteCommandSql(expression);

            SqlCapture.Current?.Capture(commandSql);

            return ExecuteAsync(connection, commandSql, cancellationToken);
        }

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        protected abstract Task<int> ExecuteAsync(IDbConnection connection, CommandSql commandSql, CancellationToken cancellationToken);
#endif
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(IDbConnection connection, Expression expression)
        {
            var commandSql = CreateReadCommandSql<T>(expression);

            SqlCapture.Current?.Capture(commandSql);

            return Query<T>(connection, commandSql);
        }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        protected abstract IEnumerable<T> Query<T>(IDbConnection connection, CommandSql commandSql);

        #if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public IAsyncEnumerable<T> QueryAsync<T>(IDbConnection connection, Expression expression)
        {
            var commandSql = CreateReadCommandSql<T>(expression);

            SqlCapture.Current?.Capture(commandSql);

            return QueryAsync<T>(connection, expression);
        }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        protected abstract IAsyncEnumerable<T> QueryAsync<T>(IDbConnection connection, CommandSql commandSql);
#endif
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public T Read<T>(IDbConnection connection, Expression expression)
        {
            var commandSql = CreateReadCommandSql<T>(expression);

            SqlCapture.Current?.Capture(commandSql);

            return Read<T>(connection, commandSql);
        }
        
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        protected abstract T Read<T>(IDbConnection connection, CommandSql<T> commandSql);

        #if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<T> ReadAsync<T>(IDbConnection connection, Expression expression, CancellationToken cancellationToken = default)
        {
            var commandSql = CreateReadCommandSql<T>(expression);

            SqlCapture.Current?.Capture(commandSql);

            return ReadAsync<T>(connection, commandSql, cancellationToken);
        }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        protected abstract Task<T> ReadAsync<T>(IDbConnection connection, CommandSql<T> commandSql, CancellationToken cancellationToken);
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
        protected virtual CommandSql<T> CreateReadCommandSql<T>(Expression expression)
        {
            using (var visitor = Create())
            {
                T defaultValue = default;

                visitor.Startup(expression);

                if (visitor.HasDefaultValue)
                {
                    if (visitor.DefaultValue is T value)
                    {
                        defaultValue = value;
                    }
                    else if (!(visitor.DefaultValue is null))
                    {
                        throw new DSyntaxErrorException($"查询结果类型({typeof(T)})和指定的默认值类型({visitor.DefaultValue.GetType()})无法进行默认转换!");
                    }
                }

                string sql = visitor.ToSQL();

                if (visitor.Required)
                {
                    return new CommandSql<T>(sql, visitor.Parameters, visitor.TimeOut, visitor.HasDefaultValue, defaultValue, visitor.MissingDataError);
                }

                return new CommandSql<T>(sql, visitor.Parameters, visitor.TimeOut, defaultValue);
            }
        }

        /// <summary>
        /// 写入命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        protected virtual CommandSql CreateWriteCommandSql(Expression expression)
        {
            using (var visitor = CreateExe())
            {
                visitor.Startup(expression);

                string sql = visitor.ToSQL();

                return new CommandSql(sql, visitor.Parameters, visitor.TimeOut);
            }
        }
    }
}
