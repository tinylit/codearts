using CodeArts.ORM.Exceptions;
using CodeArts.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Transactions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据仓储（读写）
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public class WriteRepository<T> : Repository, IExecuteable<T>, IExecuteProvider<T>, IWriteRepository<T>, IRouteExecuter<T>, IWriteRepository, IRouteExecuter where T : class, IEntiy
    {
        /// <summary>
        /// 实体表信息
        /// </summary>
        public static ITableInfo TableInfo { get; }
        static WriteRepository() => TableInfo = TableRegions.Resolve<T>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionConfig">数据库链接</param>
        /// <param name="expression">表达式</param>
        private WriteRepository(IReadOnlyConnectionConfig connectionConfig, Expression expression) : base(connectionConfig) => _Expression = expression ?? throw new ArgumentNullException(nameof(expression));

        private readonly Expression _Expression = null;

        private Expression _ContextExpression = null;

        /// <summary>
        /// 供应器。
        /// </summary>
        public IExecuteProvider<T> Provider => this;

        /// <summary>
        /// 表达式。
        /// </summary>
        public Expression Expression => _Expression ?? _ContextExpression ?? (_ContextExpression = Expression.Constant(this));

        /// <summary>
        /// 创建执行器。
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        IExecuteable<T> IExecuteProvider<T>.CreateExecute(Expression expression) => new WriteRepository<T>(ConnectionConfig, expression);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        int IExecuteProvider<T>.Execute(Expression expression) => DbExecuter.Execute(Connection, expression);

        /// <summary>
        /// 数据库执行器。
        /// </summary>
        protected virtual IDbRepositoryExecuter DbExecuter => DbConnectionManager.Create(DbAdapter);

        /// <summary>
        /// 主键生成器。
        /// </summary>
        public virtual long NewId() => KeyGen<T>.Id();

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <returns></returns>
        public IInsertable<T> AsInsertable(T entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return AsInsertable(new T[] { entry });
        }

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public IInsertable<T> AsInsertable(List<T> entries) => DbWriter<T>.AsInsertable(this, entries);

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public IInsertable<T> AsInsertable(T[] entries) => DbWriter<T>.AsInsertable(this, entries);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <returns></returns>
        public IUpdateable<T> AsUpdateable(T entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return AsUpdateable(new T[] { entry });
        }

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public IUpdateable<T> AsUpdateable(List<T> entries) => DbWriter<T>.AsUpdateable(this, entries);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public IUpdateable<T> AsUpdateable(T[] entries) => DbWriter<T>.AsUpdateable(this, entries);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <returns></returns>
        public IDeleteable<T> AsDeleteable(T entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return AsDeleteable(new T[] { entry });
        }

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public IDeleteable<T> AsDeleteable(List<T> entries) => DbWriter<T>.AsDeleteable(this, entries);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <returns></returns>
        public IDeleteable<T> AsDeleteable(T[] entries) => DbWriter<T>.AsDeleteable(this, entries);

        /// <summary>
        /// 表达式分析。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        int IWriteRepository.Excute(Expression expression) => DbExecuter.Execute(Connection, expression);

        /// <summary>
        /// 执行语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        protected int Insert(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (ExecuteAuthorize(sql, CommandTypes.Insert))
            {
                return Execute(sql, param, commandTimeout);
            }

            throw new NonAuthorizedException();
        }

        /// <summary>
        /// 执行语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        int IWriteRepository.Insert(SQL sql, object param, int? commandTimeout) => Insert(sql, param, commandTimeout);

        /// <summary>
        /// 执行语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        protected int Update(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (ExecuteAuthorize(sql, CommandTypes.Update))
            {
                return Execute(sql, param, commandTimeout);
            }

            throw new NonAuthorizedException();
        }

        /// <summary>
        /// 执行语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        int IWriteRepository.Update(SQL sql, object param, int? commandTimeout) => Update(sql, param, commandTimeout);

        /// <summary>
        /// 执行语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        protected int Delete(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (ExecuteAuthorize(sql, CommandTypes.Delete))
            {
                return Execute(sql, param, commandTimeout);
            }

            throw new NonAuthorizedException();
        }

        /// <summary>
        /// 执行语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        int IWriteRepository.Delete(SQL sql, object param, int? commandTimeout) => Delete(sql, param, commandTimeout);

        /// <summary>
        /// 执行SQL验证。
        /// </summary>
        /// <returns></returns>
        protected virtual bool ExecuteAuthorize(ISQL sql, UppercaseString commandType)
        {
            if (sql.Tables.All(x => x.CommandType == CommandTypes.Select))
            {
                return false;
            }

            return sql.Tables.All(x => x.CommandType == CommandTypes.Select || x.CommandType == commandType && string.Equals(x.Name, TableInfo.TableName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 执行SQL验证。
        /// </summary>
        /// <returns></returns>
        protected virtual bool ExecuteAuthorize(ISQL sql)
        {
            if (sql.Tables.All(x => x.CommandType == CommandTypes.Select))
            {
                return false;
            }

            return sql.Tables.All(x => x.CommandType == CommandTypes.Select || string.Equals(x.Name, TableInfo.TableName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 执行。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        private int Execute(string sql, Dictionary<string, object> param, int? commandTimeout = null)
        {
            return DbExecuter.Execute(Connection, sql, param, commandTimeout);
        }

        /// <summary>
        /// 构建参数。
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数对象</param>
        /// <returns></returns>
        protected virtual Dictionary<string, object> BuildParameters(ISQL sql, object param = null)
        {
            if (param is null)
            {
                if (sql.Parameters.Count > 0)
                {
                    throw new DSyntaxErrorException("参数少于SQL所需的参数!");
                }

                return null;
            }

            var type = param.GetType();

            if (type.IsValueType || type == typeof(string))
            {
                if (sql.Parameters.Count > 1)
                {
                    throw new DSyntaxErrorException("参数少于SQL所需的参数!");
                }

                var token = sql.Parameters.First();

                return new Dictionary<string, object>
                {
                    [token.Name] = param
                };
            }

            if (!(param is Dictionary<string, object> parameters))
            {
                parameters = param.MapTo<Dictionary<string, object>>();
            }

            if (parameters.Count < sql.Parameters.Count)
            {
                throw new DSyntaxErrorException("参数少于SQL所需的参数!");
            }

            if (!sql.Parameters.All(x => parameters.Any(y => y.Key == x.Name)))
            {
                throw new DSyntaxErrorException("参数不匹配!");
            }

            return parameters;
        }

        /// <summary>
        /// 执行。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns>影响行</returns>
        protected virtual int Execute(ISQL sql, object param = null, int? commandTimeout = null)
        {
            if (!ExecuteAuthorize(sql))
            {
                throw new NonAuthorizedException();
            }

            return Execute(sql.ToString(Settings), BuildParameters(sql, param), commandTimeout);
        }

        /// <summary>
        /// 创建路由供应器。
        /// </summary>
        /// <param name="behavior">行为。</param>
        /// <returns></returns>
        public virtual IRouteProvider<T> CreateRouteProvider(CommandBehavior behavior) => RouteProvider<T>.Instance;

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">执行超时时间。</param>
        /// <returns></returns>
        int IRouteExecuter.ExecuteCommand(string sql, Dictionary<string, object> param, int? commandTimeout)
            => Execute(sql, param, commandTimeout);
    }
}
