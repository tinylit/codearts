using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Timer = System.Timers.Timer;

namespace CodeArts.ORM
{
    /// <summary>
    /// 事务池。
    /// </summary>
    public class ThreadConnections : DesignMode.Singleton<ThreadConnections>
    {
        private bool _clearTimerRun;
        private readonly Timer _clearTimer;
        private static readonly object lockObj = new object();

        //连接池（线程及对应的数据库连接）
        private readonly static ConcurrentDictionary<Thread, Dictionary<string, ThreadConnection>> threadConnections = new ConcurrentDictionary<Thread, Dictionary<string, ThreadConnection>>();

        private class ThreadConnection : IDbConnection
        {
            private readonly IDbConnection _connection;

            public Action<ThreadConnection> Disposed { get; set; }

            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="connection">数据库链接</param>
            public ThreadConnection(IDbConnection connection) => _connection = connection;

            /// <summary>
            /// 数据库连接
            /// </summary>
            public string ConnectionString { get => _connection.ConnectionString; set => _connection.ConnectionString = value; }

            /// <summary>
            /// 连接超时时间
            /// </summary>
            public int ConnectionTimeout => _connection.ConnectionTimeout;

            /// <summary>
            /// 数据库名称
            /// </summary>
            public string Database => _connection.Database;

            /// <summary>
            /// 连接状态
            /// </summary>
            public ConnectionState State => _connection.State;

            /// <summary>
            /// 创建事务
            /// </summary>
            /// <returns></returns>
            public IDbTransaction BeginTransaction() => _connection.BeginTransaction();

            /// <summary>
            /// 创建事务
            /// </summary>
            /// <param name="il">隔离等级</param>
            /// <returns></returns>
            public IDbTransaction BeginTransaction(IsolationLevel il) => _connection.BeginTransaction(il);

            /// <summary>
            /// 修改数据库
            /// </summary>
            /// <param name="databaseName">数据库名称</param>
            public void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

            /// <summary>
            /// 打开链接
            /// </summary>
            public void Open() => _connection.Open();

            /// <summary>
            /// 关闭链接
            /// </summary>
            public void Close() => _connection.Close();

            /// <summary>
            /// 创建命令
            /// </summary>
            /// <returns></returns>
            public IDbCommand CreateCommand() => _connection.CreateCommand();

            /// <summary>
            /// 释放器不释放
            /// </summary>
            void IDisposable.Dispose() { }

            /// <summary>
            /// 释放内存
            /// </summary>
            public void Dispose() => Dispose(true);

            /// <summary>
            /// 释放内存
            /// </summary>
            /// <param name="disposing">确认释放</param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _connection?.Close();
                    _connection?.Dispose();

                    Disposed.Invoke(this);

                    GC.SuppressFinalize(this);
                }
            }
        }

        private ThreadConnections()
        {
            _clearTimer = new Timer(1000D * 60D);
            _clearTimer.Elapsed += ClearTimerElapsed;
            _clearTimer.Enabled = true;
            _clearTimer.Stop();
            _clearTimerRun = false;
        }

        private void ClearTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var connectionList = new List<Dictionary<string, ThreadConnection>>();

            lock (lockObj)
            {
                var list = new List<Thread>();

                foreach (var kv in threadConnections)
                {
                    if (kv.Key.IsAlive)
                    {
                        continue;
                    }

                    list.Add(kv.Key);
                }

                list.ForEach(thread =>
                {
                    if (threadConnections.TryRemove(thread, out Dictionary<string, ThreadConnection> value))
                    {
                        connectionList.Add(value);
                    }
                });
            }

            if (threadConnections.Count == 0)
            {
                _clearTimer.Stop();
                _clearTimerRun = false;
            }

            connectionList.ForEach(connections =>
            {
                connections.Clear();
            });
        }

        /// <summary>
        /// 链接管理。
        /// </summary>
        /// <param name="connectionString">数据库连接字符串。</param>
        /// <param name="adapter">适配器。</param>
        /// <param name="useCache">是否使用缓存。</param>
        /// <returns></returns>
        public IDbConnection GetConnection(string connectionString, IDbConnectionAdapter adapter, bool useCache = true)
        {
            Dictionary<string, ThreadConnection> connections;

            lock (lockObj)
            {
                connections = threadConnections.GetOrAdd(Thread.CurrentThread, thread =>
                {
                    return new Dictionary<string, ThreadConnection>();
                });
            }

            if (connections.TryGetValue(connectionString, out ThreadConnection threadConnection))
            {
                return threadConnection;
            }

            lock (connections)
            {
                if (connections.TryGetValue(connectionString, out threadConnection))
                {
                    return threadConnection;
                }

                threadConnection = new ThreadConnection(DispatchConnections.Instance.GetConnection(connectionString, adapter, useCache))
                {
                    Disposed = connection =>
                    {
                        lock (connections)
                        {
                            connections.Remove(connectionString);
                        }
                    }
                };

                connections.Add(connectionString, threadConnection);
            }

            if (!_clearTimerRun)
            {
                _clearTimerRun = true;
                _clearTimer.Start();
            }

            return threadConnection;
        }
    }
}
