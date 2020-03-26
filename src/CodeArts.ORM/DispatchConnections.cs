using System.Data;

namespace CodeArts.ORM
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
            /// <summary>
            /// 链接管理
            /// </summary>
            /// <param name="connectionString">数据库连接字符串</param>
            /// <param name="adapter">适配器</param>
            /// <param name="useCache">是否使用缓存</param>
            /// <returns></returns>
            public IDbConnection Create(string connectionString, IDbConnectionAdapter adapter, bool useCache = true) => adapter.Create(connectionString);
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
