using StackExchange.Redis;
using System;
using System.Collections.Concurrent;

namespace CodeArts.Cache
{
    /// <summary>
    /// Redis
    /// </summary>
    public class RedisManager : IDisposable
    {
        #region 单例

        private static readonly Lazy<RedisManager> Lazy =
            new Lazy<RedisManager>(() => new RedisManager());

        /// <summary>
        /// 实例
        /// </summary>
        public static RedisManager Instance => Lazy.Value;

        private RedisManager() => _connections = new ConcurrentDictionary<string, ConnectionMultiplexer>();

        #endregion

        #region 连接

        private readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connections;

        private ConnectionMultiplexer GetConnection(string connectionString, string name = "default") => _connections.GetOrAdd(name, p => Connect(ConfigurationOptions.Parse(connectionString)));

        private ConnectionMultiplexer Connect(ConfigurationOptions configOpts) => ConnectionMultiplexer.Connect(configOpts);

        #endregion

        /// <summary>
        /// 数据库操作对象
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="defaultDb">获取数据库的ID。</param>
        /// <returns></returns>
        public IDatabase GetDatabase(string connectionString, int defaultDb = 0) => GetConnection(connectionString).GetDatabase(defaultDb);

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            if (_connections == null || _connections.Count == 0)
            {
                return;
            }

            foreach (var conn in _connections.Values)
            {
                conn.Close();
                conn.Dispose();
            }

            _connections.Clear();
        }
    }
}
