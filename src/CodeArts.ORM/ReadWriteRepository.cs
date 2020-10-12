using CodeArts.ORM.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据仓储（读写）。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public class ReadWriteRepository<T> : ReadRepository<T>, IReadRepository<T>, IWriteRepository<T>, IRepository<T>, IOrderedQueryable<T>, IQueryable<T>, IRouteExecuter<T>, IEnumerable<T>, IReadRepository, IWriteRepository, IOrderedQueryable, IQueryable, IQueryProvider, IRouteExecuter, IEnumerable where T : class, IEntiy
    {
        /// <summary>
        /// 数据库执行器。
        /// </summary>
        protected virtual IDbRepositoryExecuter DbExecuter => DbConnectionManager.Create(DbAdapter);

        /// <summary>
        /// 主键生成器。
        /// </summary>
        public virtual long NewId() => KeyGen<T>.Id();

        /// <summary>
        /// 插入、更新、删除执行器。
        /// </summary>
        /// <returns></returns>
        public IExecuteable<T> AsExecuteable() => DbWriter<T>.AsExecuteable(this);

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
