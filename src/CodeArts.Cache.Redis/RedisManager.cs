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

        private RedisManager()
        {
            _connections = new ConcurrentDictionary<string, ConnectionMultiplexer>();
        }

        #endregion

        #region 连接

        /// <summary>
        /// Redis连接对象管理
        /// </summary>
        private readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connections;

        /// <summary>
        /// 获取Redis对象
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="name">命名</param>
        /// <returns></returns>
        private ConnectionMultiplexer GetConnection(string connectionString, string name = "default")
        {
            return _connections.GetOrAdd(name, p => Connect(ConfigurationOptions.Parse(connectionString)));
        }

        /// <summary>
        /// 连接实现
        /// </summary>
        /// <param name="configOpts"></param>
        /// <returns></returns>
        private ConnectionMultiplexer Connect(ConfigurationOptions configOpts) => ConnectionMultiplexer.Connect(configOpts);

        #endregion

        /// <summary>
        /// 数据库操作对象
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="defaultDb">默认仓储</param>
        /// <returns></returns>
        public IDatabase GetDatabase(string connectionString, int defaultDb = 0)
        {
            return GetConnection(connectionString).GetDatabase(defaultDb);
        }

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
            }
            _connections.Clear();
        }
    }
}
