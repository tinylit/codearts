using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using Timer = System.Timers.Timer;

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
            private bool _clearTimerRun;
            private readonly Timer _clearTimer;
            private readonly ConcurrentDictionary<string, List<DbConnection>> connectionCache = new ConcurrentDictionary<string, List<DbConnection>>();

            public DefaultConnections()
            {
                _clearTimer = new Timer(1000 * 60);
                _clearTimer.Elapsed += ClearTimerElapsed;
                _clearTimer.Enabled = true;
                _clearTimer.Stop();
                _clearTimerRun = false;
            }

            private void ClearTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                var list = new List<string>();
                foreach (var kv in connectionCache)
                {
                    kv.Value.RemoveAll(x =>
                    {
                        if (x.IsAlive)
                        {
                            return false;
                        }

                        x.Dispose();

                        return true;
                    });

                    if (kv.Value.Count == 0)
                    {
                        list.Add(kv.Key);
                    }
                }

                list.ForEach(key =>
                {
                    if (connectionCache.TryRemove(key, out List<DbConnection> connections) && connections.Count > 0)
                    {
                        connectionCache.TryAdd(key, connections);
                    }
                });

                if (connectionCache.Count == 0)
                {
                    _clearTimerRun = false;
                    _clearTimer.Stop();
                }
            }
            /// <summary>
            /// 链接管理
            /// </summary>
            /// <param name="connectionString">数据库连接字符串</param>
            /// <param name="adapter">适配器</param>
            /// <param name="useCache">是否使用缓存</param>
            /// <returns></returns>
            public IDbConnection Create(string connectionString, IDbConnectionAdapter adapter, bool useCache = true)
            {
                var connections = connectionCache.GetOrAdd(connectionString, _ => new List<DbConnection>());

                foreach (var item in connections)
                {
                    if (item.IsAlive && item.IsIdle)
                    {
                        return item;
                    }
                }

                var connection = new DbConnection(adapter.Create(connectionString), adapter.ConnectionHeartbeat);

                connections.Add(connection);

                if (!_clearTimerRun)
                {
                    _clearTimer.Start();
                    _clearTimerRun = true;
                }

                return connection;
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
