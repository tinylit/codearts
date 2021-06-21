using CodeArts.Db.Lts.Visitors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 仓储提供者。
    /// </summary>
    public abstract class RepositoryProvider : IDbRepositoryProvider, IDbRepositoryExecuter
    {
        private readonly ISQLCorrectSettings settings;
        private readonly ICustomVisitorList visitors;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="settings">SQL语句矫正设置。</param>
        /// <param name="visitors">自定义访问器。</param>
        public RepositoryProvider(ISQLCorrectSettings settings, ICustomVisitorList visitors)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.visitors = visitors ?? throw new ArgumentNullException(nameof(visitors));
        }

        /// <summary>
        /// 创建SQL查询器。
        /// </summary>
        /// <returns></returns>
        public virtual IQueryVisitor Create() => new QueryVisitor(settings, visitors);

        /// <summary>
        /// 创建执行器。
        /// </summary>
        /// <returns></returns>
        public virtual IExecuteVisitor CreateExe() => new ExecuteVisitor(settings, visitors);

        /// <summary>
        /// 参数适配。
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="param">参数。</param>
        protected virtual void AddParameterAuto(IDbCommand command, object param = null) => DbWriter.AddParameterAuto(command, param);

        /// <summary>
        /// 参数适配。
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="key">参数名称。</param>
        /// <param name="value">参数值。</param>
        protected virtual void AddParameterAuto(IDbCommand command, string key, ParameterValue value) => DbWriter.AddParameterAuto(command, key, value);

        /// <summary>
        /// 参数适配。
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="key">参数名称。</param>
        /// <param name="value">参数值。</param>
        protected virtual void AddParameterAuto(IDbCommand command, string key, object value) => DbWriter.AddParameterAuto(command, key, value);

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <returns></returns>
        public abstract T Read<T>(IDbContext context, CommandSql<T> commandSql);

        /// <summary>
        /// 查询列表集合。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <returns></returns>
        public abstract IEnumerable<T> Query<T>(IDbContext context, CommandSql commandSql);

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <returns>执行影响行。</returns>
        public abstract int Execute(IDbContext context, CommandSql commandSql);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>执行影响行。</returns>
        public abstract Task<int> ExecuteAsync(IDbContext context, CommandSql commandSql, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public abstract Task<T> ReadAsync<T>(IDbContext context, CommandSql<T> commandSql, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询列表集合。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <returns></returns>
        public abstract IAsyncEnumerable<T> QueryAsync<T>(IDbContext context, CommandSql commandSql);
#endif
    }
}
