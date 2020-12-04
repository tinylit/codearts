using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db
{
    /// <summary>
    /// 数据上下文。
    /// </summary>
    public class DbContext<TEntity> : Context, IDbContext<TEntity>, IDbContext, IContext where TEntity : class, IEntiy
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="connectionConfig">数据连接配置。</param>
        public DbContext(IReadOnlyConnectionConfig connectionConfig) : base(connectionConfig)
        {

        }


        private IDbRepositoryExecuter repositoryExecuter;

        /// <summary>
        /// 执行器。
        /// </summary>
        protected IDbRepositoryExecuter DbExecuter => repositoryExecuter ?? (repositoryExecuter = CreateDbExecuter());

        /// <summary>
        /// 创建适配器。
        /// </summary>
        /// <returns></returns>
        protected override IDbConnectionAdapter CreateDbAdapter(string providerName)
        {
            repositoryExecuter = null;

            return base.CreateDbAdapter(providerName);
        }

        /// <summary>
        /// 创建执行器。
        /// </summary>
        /// <returns></returns>
        protected virtual IDbRepositoryExecuter CreateDbExecuter() => DbConnectionManager.Create(DbAdapter);

        /// <summary>
        /// 写入命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        protected virtual CommandSql CreateWriteCommandSql(Expression expression)
        {
            using (var visitor = DbExecuter.CreateExe())
            {
                visitor.Startup(expression);

                string sql = visitor.ToSQL();

                return new CommandSql(sql, visitor.Parameters, visitor.TimeOut);
            }
        }

        /// <summary>
        /// 验证SQL可读性。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <returns></returns>
        protected override bool AuthorizeRead(SQL sql)
        {
            string name = DbWriter<TEntity>.TableInfo.TableName;

            if (!sql.Tables.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new NonAuthorizedException($"在当前数据库上下文未对“{name}”表做任何操作！");
            }

            return base.AuthorizeRead(sql);
        }

        /// <summary>
        /// 验证SQL可写性。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <returns></returns>
        protected virtual bool AuthorizeWrite(SQL sql)
        {
            bool flag = false;

            string name = DbWriter<TEntity>.TableInfo.TableName;

            foreach (var token in sql.Tables)
            {
                if (token.CommandType == CommandTypes.Select)
                {
                    continue;
                }

                if (string.Equals(token.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    flag = true;

                    continue;
                }

                throw new NonAuthorizedException($"禁止在当前数据库上下文对“{token.Name}”表做非查询操作！");
            }

            return flag;
        }

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public int Execute(Expression expression)
        {
            var commandSql = CreateWriteCommandSql(expression);

            SqlCapture.Current?.Capture(commandSql);

            return DbExecuter.Execute(this, commandSql);
        }

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns>执行影响行。</returns>
        public int Execute(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (!AuthorizeWrite(sql))
            {
                throw new NonAuthorizedException();
            }

            var commandSql = new CommandSql(sql.ToString(Settings), PreparationParameters(sql, param), commandTimeout);

            SqlCapture.Current?.Capture(commandSql);

            return DbExecuter.Execute(this, commandSql);
        }

#if NET_NORMAL
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
        {
            var commandSql = CreateWriteCommandSql(expression);

            SqlCapture.Current?.Capture(commandSql);

            return DbExecuter.ExecuteAsync(this, commandSql, cancellationToken);
        }

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>执行影响行。</returns>
        public Task<int> ExecuteAsync(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            if (!AuthorizeWrite(sql))
            {
                throw new NonAuthorizedException();
            }

            var commandSql = new CommandSql(sql.ToString(Settings), PreparationParameters(sql, param), commandTimeout);

            SqlCapture.Current?.Capture(commandSql);

            return DbExecuter.ExecuteAsync(this, commandSql, cancellationToken);
        }

#endif
    }
}
