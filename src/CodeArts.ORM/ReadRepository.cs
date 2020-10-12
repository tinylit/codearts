using CodeArts.ORM.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据仓储（只读）。
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public class ReadRepository<T> : Repository<T>, IReadRepository<T>, IRepository<T>, IOrderedQueryable<T>, IQueryable<T>, IEnumerable<T>, IReadRepository, IOrderedQueryable, IQueryable, IQueryProvider, IEnumerable where T : class, IEntiy
    {
        /// <summary>
        /// 实体表信息。
        /// </summary>
        public static ITableInfo TableInfo { get; }

        static ReadRepository() => TableInfo = TableRegions.Resolve<T>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ReadRepository() : base()
        {
        }

        /// <summary>
        /// 查询SQL验证。
        /// </summary>
        /// <returns></returns>
        protected virtual bool QueryAuthorize(ISQL sql) => sql.Tables.All(x => x.CommandType == CommandTypes.Select) && sql.Tables.Any(x => string.Equals(x.Name, TableInfo.TableName, StringComparison.OrdinalIgnoreCase));

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
        /// 查询一条数据(未查询到数据)。
        /// </summary>
        /// <typeparam name="TResult">结果。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        protected virtual TResult QueryFirstOrDefault<TResult>(SQL sql, object param = null, int? commandTimeout = null, TResult defaultValue = default)
        {
            if (!QueryAuthorize(sql))
            {
                throw new NonAuthorizedException();
            }

            return DbProvider.QueryFirstOrDefault<TResult>(Connection, sql.ToString(Settings), BuildParameters(sql, param), commandTimeout, defaultValue);
        }

        TResult IReadRepository.QueryFirstOrDefault<TResult>(SQL sql, object param, int? commandTimeout, TResult defaultValue) => QueryFirstOrDefault(sql, param, commandTimeout, defaultValue);

        /// <summary>
        /// 查询一条数据。
        /// </summary>
        /// <typeparam name="TResult">结果。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="hasDefaultValue">含默认值。</param>
        /// <param name="defaultValue">默认值（仅“<paramref name="hasDefaultValue"/>”为真时，有效）。</param>
        /// <param name="missingMsg">未查询到数据时，异常信息。</param>
        /// <returns></returns>
        protected virtual TResult QueryFirst<TResult>(SQL sql, object param = null, int? commandTimeout = null, bool hasDefaultValue = false, TResult defaultValue = default, string missingMsg = null)
        {
            if (!QueryAuthorize(sql))
            {
                throw new NonAuthorizedException();
            }

            return DbProvider.QueryFirst<TResult>(Connection, sql.ToString(Settings), BuildParameters(sql, param), commandTimeout, hasDefaultValue, defaultValue, missingMsg);
        }

        TResult IReadRepository.QueryFirst<TResult>(SQL sql, object param, int? commandTimeout, bool hasDefaultValue, TResult defaultValue, string missingMsg) => QueryFirst(sql, param, commandTimeout, hasDefaultValue, defaultValue, missingMsg);

        /// <summary>
        /// 查询所有数据。
        /// </summary>
        /// <typeparam name="TResult">结果。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        protected virtual IEnumerable<TResult> Query<TResult>(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (!QueryAuthorize(sql))
            {
                throw new NonAuthorizedException();
            }

            return DbProvider.Query<TResult>(Connection, sql.ToString(Settings), BuildParameters(sql, param), commandTimeout);

        }

        IEnumerable<TResult> IReadRepository.Query<TResult>(SQL sql, object param, int? commandTimeout) => Query<TResult>(sql, param, commandTimeout);
    }
}
