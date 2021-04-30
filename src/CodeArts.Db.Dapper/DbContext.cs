using Dapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeArts.Db.Dapper
{
    /// <summary>
    /// 数据上下文。
    /// </summary>
    public class DbContext
    {
        private readonly IReadOnlyConnectionConfig connectionConfig;
        private static readonly ConcurrentDictionary<Type, DbConfigAttribute> DbConfigCache = new ConcurrentDictionary<Type, DbConfigAttribute>();
        private static readonly Regex PatternColumn = new Regex(@"\bselect[\x20\t\r\n\f]+(?<cols>((?!\b(select|where)\b)[\s\S])+(select((?!\b(from|select)\b)[\s\S])+from((?!\b(from|select)\b)[\s\S])+)*((?!\b(from|select)\b)[\s\S])*)[\x20\t\r\n\f]+from[\x20\t\r\n\f]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PatternOrderBy = new Regex(@"[\x20\t\r\n\f]+order[\x20\t\r\n\f]+by[\x20\t\r\n\f]+((?!\b(select|where)\b)[\s\S])+(select((?!\b(from|select)\b)[\s\S])+from((?!\b(from|select)\b)[\s\S])+)*((?!\b(from|select)\b)[\s\S])*$", RegexOptions.IgnoreCase | RegexOptions.RightToLeft | RegexOptions.Compiled);

        /// <summary>
        /// inheritdoc
        /// </summary>
        public DbContext()
        {
            this.connectionConfig = GetDbConfig();
        }

        /// <summary>
        /// inheritdoc
        /// </summary>
        public DbContext(IReadOnlyConnectionConfig connectionConfig)
        {
            this.connectionConfig = connectionConfig ?? throw new ArgumentNullException(nameof(connectionConfig));
        }

        /// <summary>
        /// 获取数据库配置。
        /// </summary>
        /// <returns></returns>
        protected virtual IReadOnlyConnectionConfig GetDbConfig()
        {
            var attr = DbConfigCache.GetOrAdd(GetType(), type =>
            {
                return (DbConfigAttribute)Attribute.GetCustomAttribute(type, typeof(DbConfigAttribute));
            });

            return attr.GetConfig();
        }

        /// <summary> 连接名称。 </summary>
        public string Name => connectionConfig.Name;

        /// <summary> 数据库驱动名称。 </summary>
        public string ProviderName => connectionConfig.ProviderName;

        /// <summary>
        /// SQL 矫正。
        /// </summary>
        public ISQLCorrectSettings Settings => DbAdapter.Settings;

        private IDbConnectionAdapter connectionAdapter;

        /// <summary>
        /// 适配器。
        /// </summary>
        protected IDbConnectionAdapter DbAdapter
        {
            get
            {
                if (connectionAdapter is null || !string.Equals(connectionAdapter.ProviderName, connectionConfig.ProviderName))
                {
                    connectionAdapter = CreateDbAdapter(connectionConfig.ProviderName);
                }

                return connectionAdapter;
            }
        }

        /// <summary>
        /// 创建适配器。
        /// </summary>
        /// <returns></returns>
        protected virtual IDbConnectionAdapter CreateDbAdapter(string providerName) => DapperConnectionManager.Get(providerName);

        /// <summary>
        /// 创建数据库查询器。
        /// </summary>
        /// <param name="useCache">优先复用链接池，否则：始终创建新链接。</param>
        /// <returns></returns>
        protected virtual IDbConnection CreateDb(bool useCache = true) => TransactionConnections.GetConnection(connectionConfig.ConnectionString, DbAdapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, DbAdapter, useCache);

        /// <summary>
        /// 查询第一行第一列结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public T ExecuteScalar<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            using (IDbConnection connection = CreateDb())
            {
                return connection.ExecuteScalar<T>(sql.ToString(Settings), param, commandTimeout: commandTimeout);
            }
        }

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public T QueryFirst<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            using (IDbConnection connection = CreateDb())
            {
                return connection.QueryFirst<T>(sql.ToString(Settings), param, commandTimeout: commandTimeout);
            }
        }

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public T QueryFirstOrDefault<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            using (IDbConnection connection = CreateDb())
            {
                return connection.QueryFirstOrDefault<T>(sql.ToString(Settings), param, commandTimeout: commandTimeout);
            }
        }

        /// <summary>
        /// 查询列表集合。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            using (IDbConnection connection = CreateDb())
            {
                return connection.Query<T>(sql.ToString(Settings), param, commandTimeout: commandTimeout);
            }
        }

        /// <summary>
        /// 转分页SQL（countSql;listSql）。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <param name="pageIndex">页面索引。</param>
        /// <param name="pageSize">分页条数。</param>
        /// <returns></returns>
        protected virtual string ToPagedSql(SQL sql, int pageIndex, int pageSize)
        {
            var settings = DbAdapter.Settings;

            var sqlStr = sql.ToString(settings);

            var colsMt = PatternColumn.Match(sqlStr);

            if (!colsMt.Success)
            {
                throw new NotSupportedException();
            }

            var colsGrp = colsMt.Groups["cols"];

            var orderByMt = PatternOrderBy.Match(sqlStr);

            var sb = new StringBuilder();

#if NETSTANDARD2_1
            sb.Append(sqlStr[..colsGrp.Index])
#else
            sb.Append(sqlStr.Substring(0, colsGrp.Index))
#endif
                .Append("COUNT(1)");

            int subIndex = colsGrp.Index + colsGrp.Value.Length;

            if (orderByMt.Success)
            {
#if NETSTANDARD2_1
                sb.Append(sqlStr[subIndex..orderByMt.Index]);
#else
                sb.Append(sqlStr.Substring(subIndex, orderByMt.Index - subIndex));
#endif
            }
            else
            {
#if NETSTANDARD2_1
                sb.Append(sqlStr[subIndex..]);
#else
                sb.Append(sqlStr.Substring(subIndex));
#endif
            }

            return sb.Append(";")
                 .Append(orderByMt.Success
#if NETSTANDARD2_1
                 ? settings.ToSQL(sqlStr[..orderByMt.Index], pageSize, pageSize * pageIndex, sqlStr[orderByMt.Index..])
#else
                 ? settings.ToSQL(sqlStr.Substring(0, orderByMt.Index), pageSize, pageSize * pageIndex, sqlStr.Substring(orderByMt.Index))
#endif
                 : settings.ToSQL(sqlStr, pageSize, pageSize * pageIndex, string.Empty)
                 ).ToString();
        }

        /// <summary>
        /// 查询分页数据。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public PagedList<T> Query<T>(SQL sql, int pageIndex, int pageSize, object param = null, int? commandTimeout = null)
        {
            var sqlStr = ToPagedSql(sql, pageIndex, pageSize);

            using (IDbConnection connection = CreateDb())
            {
                using (var reader = connection.QueryMultiple(sqlStr, param, commandTimeout: commandTimeout))
                {
                    int count = reader.ReadFirst<int>();

                    var results = reader.Read<T>();

                    return new PagedList<T>(results as List<T> ?? results.ToList(), pageIndex, pageSize, count);
                }
            }
        }

        /// <summary>
        /// 执行命令，返回影响行数。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public int Execute(SQL sql, object param = null, int? commandTimeout = null)
        {
            using (IDbConnection connection = CreateDb())
            {
                return connection.Execute(sql.ToString(Settings), param, commandTimeout: commandTimeout);
            }
        }

#if NET_NORMAL || NET_CORE

        /// <summary>
        /// 查询第一行第一列结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public async Task<T> ExecuteScalarAsync<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            using (IDbConnection connection = CreateDb())
            {
                return await connection.ExecuteScalarAsync<T>(sql.ToString(Settings), param, commandTimeout: commandTimeout).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public async Task<T> QueryFirstAsync<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            using (IDbConnection connection = CreateDb())
            {
                return await connection.QueryFirstAsync<T>(sql.ToString(Settings), param, commandTimeout: commandTimeout).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public async Task<T> QueryFirstOrDefaultAsync<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            using (IDbConnection connection = CreateDb())
            {
                return await connection.QueryFirstOrDefaultAsync<T>(sql.ToString(Settings), param, commandTimeout: commandTimeout).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 查询列表集合。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> QueryAsync<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            using (IDbConnection connection = CreateDb())
            {
                return await connection.QueryAsync<T>(sql.ToString(Settings), param, commandTimeout: commandTimeout).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 查询分页数据。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public async Task<PagedList<T>> QueryAsync<T>(SQL sql, int pageIndex, int pageSize, object param = null, int? commandTimeout = null)
        {
            var sqlStr = ToPagedSql(sql, pageIndex, pageSize);

            using (IDbConnection connection = CreateDb())
            {
                using (var reader = await connection.QueryMultipleAsync(sqlStr, param, commandTimeout: commandTimeout).ConfigureAwait(false))
                {
                    int count = await reader.ReadFirstAsync<int>().ConfigureAwait(false);

                    var results = await reader.ReadAsync<T>().ConfigureAwait(false);

                    return new PagedList<T>(results as List<T> ?? results.ToList(), pageIndex, pageSize, count);
                }
            }
        }

        /// <summary>
        /// 执行命令，返回影响行数。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public async Task<int> ExecuteAsync(SQL sql, object param = null, int? commandTimeout = null)
        {
            using (IDbConnection connection = CreateDb())
            {
                return await connection.ExecuteAsync(sql.ToString(Settings), param, commandTimeout: commandTimeout).ConfigureAwait(false);
            }
        }
#endif
    }
}
