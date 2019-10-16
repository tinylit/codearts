using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Timer = System.Timers.Timer;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 线程链接池
    /// </summary>
    public class ThreadScopeConnections : DesignMode.Singleton<ThreadScopeConnections>
    {
        //连接池（线程及对应的数据库连接）
        private readonly static ConcurrentDictionary<Thread, Dictionary<string, DbConnectionWrapper>> ConnectionCache;

        //失效定时器
        private readonly Timer _clearTimer;

        private static readonly object LockObj = new object();

        private bool _clearTimerRun;


        /// <summary>
        /// 初始构造
        /// </summary>
        static ThreadScopeConnections()
        {
            ConnectionCache = new ConcurrentDictionary<Thread, Dictionary<string, DbConnectionWrapper>>();
        }


        /// <summary>
        /// 初始构造
        /// </summary>
        private ThreadScopeConnections()
        {
            _clearTimer = new Timer(1000 * 60);  //? 每分钟清理一次
            _clearTimer.Elapsed += ClearTimerElapsed;
            _clearTimer.Stop();
            _clearTimerRun = false;
        }


        /// <summary>
        /// 清理事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ClearDict();
        }


        /// <summary>
        /// 清理失效的线程级缓存
        /// </summary>
        private void ClearDict()
        {
            var clearConnThread = new List<Thread>();
            var clearConn = new Dictionary<string, DbConnectionWrapper>();
            lock (LockObj)
            {
                //标记
                foreach (var entry in ConnectionCache)
                {
                    foreach (var kv in entry.Value)
                    {
                        //线程存活，数据库连接存活
                        if (entry.Key.IsAlive && kv.Value.IsAlive)
                            continue;

                        //添加删除信息
                        clearConn.Add(kv.Key, kv.Value);
                    }

                    foreach (var kv in clearConn)
                    {
                        kv.Value.Dispose();
                        entry.Value.Remove(kv.Key);
                    }

                    if (entry.Value.Count == 0)
                        clearConnThread.Add(entry.Key);
                }
                foreach (var item in clearConnThread)
                {
                    ConnectionCache.TryRemove(item, out _);
                }

                if (ConnectionCache.Count == 0)
                {
                    _clearTimerRun = false;
                    _clearTimer.Stop();
                }
            }
        }

        /// <summary> 获取数据库连接 </summary>
        /// <param name="connectionString">数据库连接</param>
        /// <param name="adapter">数据库适配器</param>
        /// <param name="threadCache">是否启用线程缓存(不启用缓存时优先读取事务连接池)</param>
        /// <returns></returns>
        public IDbConnection GetConnection(string connectionString, IDbConnectionAdapter adapter, bool threadCache = true)
        {
            lock (LockObj)
            {
                if (!threadCache)
                    return TransactionScopeConnections.GetConnection(connectionString, adapter) ?? adapter.Create(connectionString);

                var connDict = ConnectionCache.GetOrAdd(Thread.CurrentThread, thread => new Dictionary<string, DbConnectionWrapper>());

                if (!connDict.TryGetValue(connectionString, out DbConnectionWrapper info))
                {
                    if (!_clearTimerRun)
                    {
                        _clearTimer.Start();
                        _clearTimerRun = true;
                    }

                    connDict.Add(connectionString, info = new DbConnectionWrapper(adapter.Create(connectionString), TimeSpan.FromMinutes(Math.Min(60D, Math.Max(5D, adapter.ConnectionHeartbeat)))));
                }

                return info.GetConnection();
            }
        }

        /// <summary> 数据库连接缓存总数/// </summary>
        public int Count => ConnectionCache.Sum(t => t.Value.Count);

        /// <summary>
        /// 清空链接池
        /// </summary>
        public void Clear()
        {
            var clearConnThread = new List<Thread>();
            var clearConn = new Dictionary<string, DbConnectionWrapper>();
            lock (LockObj)
            {
                foreach (var key in ConnectionCache.Keys)
                {
                    if (!ConnectionCache.TryGetValue(key, out Dictionary<string, DbConnectionWrapper> connDict))
                        continue;
                    foreach (var kv in connDict)
                    {
                        clearConn.Add(kv.Key, kv.Value);
                    }
                    clearConnThread.Add(key);
                    foreach (var kv in clearConn)
                    {
                        kv.Value.Dispose();
                        connDict.Remove(kv.Key);
                    }
                }
                foreach (var item in clearConnThread)
                {
                    ConnectionCache.TryRemove(item, out _);
                }
            }
        }
    }
}
