using SkyBuilding.ORM.Builders;
using SkyBuilding.ORM.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 仓储提供者
    /// </summary>
    public abstract class RepositoryProvider : IDbRepositoryProvider, IDbRepositoryExecuter
    {
        private readonly static AsyncLocal<bool> localCache = new AsyncLocal<bool>();

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
        public virtual IBuilder Create() => new QueryBuilder(Settings);

        /// <summary>
        /// 创建执行器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual IBuilder<T> Create<T>() => new ExecuteBuilder<T>(Settings);

        /// <summary>
        /// 查询独立实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">查询语句</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        protected abstract T Single<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters);

        /// <summary>
        /// 查询列表集合
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">查询语句</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        protected abstract IEnumerable<T> Select<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters);

        /// <summary>
        /// 测评表达式语句（查询）
        /// </summary>
        /// <typeparam name="T">仓储泛型类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="connection">数据库链接</param>
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
                        return (TResult)Select<T>(conn, sql, builder.Parameters);
                    }
                    return Single<TResult>(conn, sql, builder.Parameters);
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
                    lock (conn)
                    {
                        return Execute(conn, sql, builder.Parameters);
                    }
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
        public abstract int Execute(IDbConnection conn, string sql, Dictionary<string, object> parameters);
    }
}
