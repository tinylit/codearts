using CodeArts.ORM.Builders;
using CodeArts.ORM.Exceptions;
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

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="settings">SQL语句矫正设置</param>
        public RepositoryProvider(ISQLCorrectSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// SQL语句矫正设置
        /// </summary>
        protected ISQLCorrectSettings Settings { private set; get; }

        /// <summary>
        /// 创建SQL查询器
        /// </summary>
        /// <returns></returns>
        public virtual IQueryBuilder Create() => new QueryBuilder(Settings);

        /// <summary>
        /// 创建执行器
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <returns></returns>
        public virtual IExecuteBuilder<T> Create<T>() => new ExecuteBuilder<T>(Settings);

        /// <summary>
        /// 查询独立实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">查询语句</param>
        /// <param name="parameters">参数</param>
        /// <param name="required">是否必须</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="commandTimeout">执行超时时间</param>
        /// <param name="missingMsg">未查询到数据时的异常信息</param>
        /// <returns></returns>
        public abstract T QueryFirst<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, bool required = false, T defaultValue = default, int? commandTimeout = null, string missingMsg = null);

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

            using (var builder = Create())
            {
                localCache.Value = true;

                try
                {
                    builder.Evaluate(expression);
                }
                finally
                {
                    localCache.Value = false;
                }

                string sql = builder.ToSQL();

                try
                {
                    if (typeof(IEnumerable<T>).IsAssignableFrom(typeof(TResult)))
                    {
                        return (TResult)Query<T>(conn, sql, builder.Parameters, builder.TimeOut);
                    }

                    if (builder.HasDefaultValue)
                    {
                        object value = builder.DefaultValue;

                        if (value is TResult defaultValue)
                        {
                            return QueryFirst(conn, sql, builder.Parameters, builder.Required, defaultValue, builder.TimeOut, builder.MissingDataError);
                        }

                        Type conversionType = typeof(TResult);

                        if (value is null)
                        {
                            if (conversionType.IsValueType)
                            {
                                throw new DSyntaxErrorException($"查询结果类型({conversionType})和指定的默认值(null)无法进行默认转换!");
                            }

                            return QueryFirst<TResult>(conn, sql, builder.Parameters, builder.Required, default, builder.TimeOut, builder.MissingDataError);
                        }

                        throw new DSyntaxErrorException($"查询结果类型({conversionType})和指定的默认值类型({value.GetType()})无法进行默认转换!");
                    }

                    return QueryFirst<TResult>(conn, sql, builder.Parameters, builder.Required, default, builder.TimeOut, builder.MissingDataError);
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
        public int Execute<T>(IDbConnection conn, Expression expression)
        {
            if (localCache.Value)
            {
                throw new NotSupportedException("禁止在执行器分析中执行查询操作，如果必须，请将表达式查询结果用变量存储，再作为条件语句的一部分！");
            }

            using (var builder = Create<T>())
            {
                localCache.Value = true;

                try
                {
                    builder.Evaluate(expression);
                }
                finally
                {
                    localCache.Value = false;
                }

                string sql = builder.ToSQL();

                try
                {
                    return Execute(conn, sql, builder.Parameters, builder.TimeOut);
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
