using CodeArts.ORM.Exceptions;
using CodeArts.ORM.Visitors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading;

namespace CodeArts.ORM
{
    /// <summary>
    /// 仓储提供者
    /// </summary>
    public abstract class RepositoryProvider : IDbRepositoryProvider, IDbRepositoryExecuter
    {
        private static readonly AsyncLocal<bool> localCache = new AsyncLocal<bool>();
        private readonly ISQLCorrectSettings settings;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settings">SQL语句矫正设置</param>
        public RepositoryProvider(ISQLCorrectSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// 创建SQL查询器
        /// </summary>
        /// <returns></returns>
        public virtual IQueryVisitor Create() => new QueryVisitor(settings);

        /// <summary>
        /// 创建执行器。
        /// </summary>
        /// <returns></returns>
        public virtual IExecuteVisitor CreateExe() => new ExecuteVisitor(settings);

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="conn">数据库链接。</param>
        /// <param name="sql">SQL。</param>
        /// <param name="parameters">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        public abstract T QueryFirstOrDefault<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, int? commandTimeout = null, T defaultValue = default);

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="conn">数据库链接。</param>
        /// <param name="sql">SQL。</param>
        /// <param name="parameters">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="hasDefaultValue">是否包含默认值。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <param name="missingMsg">未查询到数据时，异常信息。</param>
        /// <returns></returns>
        public abstract T QueryFirst<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, int? commandTimeout = null, bool hasDefaultValue = false, T defaultValue = default, string missingMsg = null);

        /// <summary>
        /// 查询列表集合
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">查询语句</param>
        /// <param name="parameters">参数</param>
        /// <param name="commandTimeout">执行超时时间</param>
        /// <returns></returns>
        public abstract IEnumerable<T> Query<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, int? commandTimeout = null);

        /// <summary>
        /// 测评表达式语句（查询）
        /// </summary>
        /// <typeparam name="T">仓储泛型类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="expression">查询表达式</param>
        /// <returns></returns>
        public TResult Evaluate<T, TResult>(IDbConnection conn, Expression expression)
        {
            if (localCache.Value)
            {
                throw new NotSupportedException("禁止在查询器分析中执行查询操作，如果必须，请将表达式查询结果用变量存储，再作为条件语句的一部分！");
            }

            var sqlProfiler = SqlCapture.Current;

            using (var visitor = Create())
            {
                localCache.Value = true;

                try
                {
                    visitor.Startup(expression);
                }
                finally
                {
                    localCache.Value = false;
                }

                string sql = visitor.ToSQL();

                sqlProfiler?.Capture(sql, visitor.Parameters, CommandBehavior.Select);

                try
                {
                    if (typeof(IEnumerable<T>).IsAssignableFrom(typeof(TResult)))
                    {
                        return (TResult)Query<T>(conn, sql, visitor.Parameters, visitor.TimeOut);
                    }

                    TResult defaultValue = default;

                    if (visitor.HasDefaultValue)
                    {
                        if (visitor.DefaultValue is TResult value)
                        {
                            defaultValue = value;
                        }
                        else
                        {
                            Type conversionType = typeof(TResult);

                            if (visitor.DefaultValue is null)
                            {
                                if (conversionType.IsValueType)
                                {
                                    throw new DSyntaxErrorException($"查询结果类型({conversionType})和指定的默认值(null)无法进行默认转换!");
                                }
                            }
                            else
                            {
                                throw new DSyntaxErrorException($"查询结果类型({conversionType})和指定的默认值类型({visitor.DefaultValue.GetType()})无法进行默认转换!");
                            }
                        }
                    }

                    if (visitor.Required)
                    {
                        return QueryFirst(conn, sql, visitor.Parameters, visitor.TimeOut, visitor.HasDefaultValue, defaultValue, visitor.MissingDataError);
                    }

                    return QueryFirstOrDefault(conn, sql, visitor.Parameters, visitor.TimeOut, defaultValue);
                }
                catch (DbException db)
                {
                    throw new DException(sql, db);
                }
            }
        }

        /// <summary>
        /// 执行增删改功能
        /// </summary>
        /// <param name="conn">数据库链接</param>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        public int Execute(IDbConnection conn, Expression expression)
        {
            if (localCache.Value)
            {
                throw new NotSupportedException("禁止在执行器分析中执行查询操作，如果必须，请将表达式查询结果用变量存储，再作为条件语句的一部分！");
            }

            var sqlProfiler = SqlCapture.Current;

            using (var visitor = CreateExe())
            {
                localCache.Value = true;

                try
                {
                    visitor.Startup(expression);
                }
                finally
                {
                    localCache.Value = false;
                }

                string sql = visitor.ToSQL();

                sqlProfiler?.Capture(sql, visitor.Parameters, visitor.Behavior);

                try
                {
                    return Execute(conn, sql, visitor.Parameters, visitor.TimeOut);
                }
                catch (DbException db)
                {
                    throw new DException(sql, db);
                }
            }
        }

        /// <summary>
        /// 执行增删改功能
        /// </summary>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">执行语句</param>
        /// <param name="parameters">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        public abstract int Execute(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, int? commandTimeout = null);
    }
}
