using System.Collections.Concurrent;
using System.Data;
using System.Web;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 管理连接
    /// </summary>
    public class DispatchConnections : DesignMode.Singleton<DispatchConnections>
    {
        private static IDispatchConnections _connections;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static DispatchConnections() => _connections = RuntimeServManager.Singleton<IDispatchConnections, DefaultConnections>(connections => _connections = connections);

        /// <summary>
        /// 默认链接
        /// </summary>
        private class DefaultConnections : IDispatchConnections
        {
#if NET45 || NET451 || NET452 || NET461
            private static readonly ConcurrentDictionary<HttpContext, DbConnection> ConnectionCache = new ConcurrentDictionary<HttpContext, DbConnection>();
#endif
            /// <summary>
            /// 链接管理
            /// </summary>
            /// <param name="connectionString"></param>
            /// <param name="adapter"></param>
            /// <param name="useCache"></param>
            /// <returns></returns>
            public IDbConnection Create(string connectionString, IDbConnectionAdapter adapter, bool useCache = true)
            {
#if NET40 || NETSTANDARD2_0
                return adapter.Create(connectionString);
#else

                if (!useCache || HttpContext.Current is null)
                    return adapter.Create(connectionString);

                return ConnectionCache.GetOrAdd(HttpContext.Current, context =>
                 {
                     context.AddOnRequestCompleted(context2 =>
                     {
                         if (ConnectionCache.TryRemove(context2, out DbConnection connection))
                         {
                             connection.Dispose();
                         }
                     });

                     return new DbConnection(adapter.Create(connectionString));
                 });
#endif

            }
        }

        /// <summary>
        /// 获取数据库连接
        /// </summary>
        /// <param name="connectionString">链接字符串</param>
        /// <param name="adapter">数据库适配器</param>
        /// <param name="useCache">使用缓存</param>
        /// <returns></returns>
        public IDbConnection GetConnection(string connectionString, IDbConnectionAdapter adapter, bool useCache = true) => _connections.Create(connectionString, adapter, useCache);
    }
}
