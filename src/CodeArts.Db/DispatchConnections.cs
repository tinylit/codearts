using CodeArts.Db.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
#if NET40 ||NET_NORMAL
using System.Runtime.Remoting;
#endif
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace CodeArts.Db
{
    /// <summary>
    /// 管理连接。
    /// </summary>
    public class DispatchConnections : Singleton<DispatchConnections>
    {
        private static readonly IDispatchConnections _connections;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static DispatchConnections() => _connections = RuntimeServPools.Singleton<IDispatchConnections, DefaultConnections>();

        private interface IDispatchConnection : IDbConnection
        {
            /// <summary>
            /// 线程是否存活。
            /// </summary>
            bool IsThreadActive { get; }

            /// <summary>
            /// 是否存活。
            /// </summary>
            bool IsAlive { get; }

            /// <summary>
            /// 是否活跃。
            /// </summary>
            bool IsActive { get; }

            /// <summary>
            /// 是否已释放。
            /// </summary>
            bool IsReleased { get; }

            /// <summary>
            /// 活动时间。
            /// </summary>
            DateTime ActiveTime { get; }

            /// <summary>
            /// 释放。
            /// </summary>
            void Destroy();

            /// <summary>
            /// 复用。
            /// </summary>
            /// <returns></returns>
            IDbConnection ReuseConnection();
        }

        private class DbConnection : IDispatchConnection
        {
            private readonly IDbConnection connection; //数据库连接
            private readonly double? connectionHeartbeat; //心跳
            private readonly bool useCache;
            private ConnectionState connectionState = ConnectionState.Closed;
            private Thread isActiveThread = Thread.CurrentThread;
            public DbConnection(IDbConnection connection, double connectionHeartbeat, bool useCache)
            {
                this.connection = connection;
                this.connectionHeartbeat = new double?(connectionHeartbeat);
                this.useCache = useCache;
            }

            public string ConnectionString { get => connection.ConnectionString; set => connection.ConnectionString = value; }

            public int ConnectionTimeout => connection.ConnectionTimeout;

            public string Database => connection.Database;

            public ConnectionState State => connectionState == ConnectionState.Closed ? connectionState : connection.State;

            public IDbTransaction BeginTransaction() => connection.BeginTransaction();

            public IDbTransaction BeginTransaction(IsolationLevel il) => connection.BeginTransaction(il);

            public void ChangeDatabase(string databaseName) => connection.ChangeDatabase(databaseName);

            public void Open()
            {
                switch (connection.State)
                {
                    case ConnectionState.Closed:
                        connection.Open();
                        break;
                    case ConnectionState.Connecting:

                        do
                        {
                            Thread.Sleep(5);

                        } while (connection.State == ConnectionState.Connecting);

                        goto default;
                    case ConnectionState.Broken:
                        connection.Close();
                        goto default;
                    default:
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }
                        break;
                }

                connectionState = connection.State;
            }

            public void Close()
            {
                connectionState = ConnectionState.Closed;

                if (connection.State == ConnectionState.Closed)
                {
                    return;
                }

                if (connectionHeartbeat.HasValue && DateTime.Now <= ActiveTime.AddMinutes(connectionHeartbeat.Value))
                {
                    return;
                }

                connection.Close();
            }

            public IDbCommand CreateCommand() => connection.CreateCommand();

            public bool IsThreadActive => isActiveThread.IsAlive;

            public bool IsAlive => connection.State == ConnectionState.Open;

            public bool IsActive { get; private set; } = true;

            public bool IsReleased { private set; get; }

            public DateTime ActiveTime { get; private set; } = DateTime.Now;

            public void Dispose()
            {
                IsActive = false;

                if (useCache)
                {
                    Close();
                }
                else
                {
                    Destroy();
                }
            }

            public IDbConnection ReuseConnection()
            {
                connectionState = ConnectionState.Closed;

                isActiveThread = Thread.CurrentThread;

                ActiveTime = DateTime.Now;

                IsActive = true;

                return this;
            }

            public void Destroy() => Dispose(true);

            protected void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (IsReleased)
                    {
                        return;
                    }

                    IsReleased = true;

                    connection?.Close();
                    connection?.Dispose();

                    GC.SuppressFinalize(this);
                }
            }
        }

        private class DispatchConnection : System.Data.Common.DbConnection, IDispatchConnection
        {
            private readonly System.Data.Common.DbConnection connection;

            private readonly double? connectionHeartbeat; //心跳
            private readonly bool useCache;
            private ConnectionState connectionState = ConnectionState.Closed;
            private Thread isActiveThread = Thread.CurrentThread;

            public DispatchConnection(System.Data.Common.DbConnection connection, double connectionHeartbeat, bool useCache)
            {
                this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
                this.connectionHeartbeat = new double?(connectionHeartbeat);
                this.useCache = useCache;
            }

            public override string ConnectionString { get => connection.ConnectionString; set => connection.ConnectionString = value; }

            public override string Database => connection.Database;

            public override string DataSource => connection.DataSource;

            public override string ServerVersion => connection.ServerVersion;

            public override int ConnectionTimeout => connection.ConnectionTimeout;

#if NET40 ||NET_NORMAL
            public override ObjRef CreateObjRef(Type requestedType) => connection.CreateObjRef(requestedType);
#endif

            public override object InitializeLifetimeService() => connection.InitializeLifetimeService();

            public override DataTable GetSchema() => connection.GetSchema();

            public override DataTable GetSchema(string collectionName) => connection.GetSchema(collectionName);

            public override DataTable GetSchema(string collectionName, string[] restrictionValues) => connection.GetSchema(collectionName, restrictionValues);

            public override ISite Site { get => connection.Site; set => connection.Site = value; }

            public override event StateChangeEventHandler StateChange { add { connection.StateChange += value; } remove { connection.StateChange -= value; } }

            public override ConnectionState State => connectionState == ConnectionState.Closed ? connectionState : connection.State;

            public override void ChangeDatabase(string databaseName) => connection.ChangeDatabase(databaseName);
            public override void EnlistTransaction(System.Transactions.Transaction transaction) => connection.EnlistTransaction(transaction);

            public override void Close()
            {
                connectionState = ConnectionState.Closed;

                if (connection.State == ConnectionState.Closed)
                {
                    return;
                }

                if (connectionHeartbeat.HasValue && DateTime.Now <= ActiveTime.AddMinutes(connectionHeartbeat.Value))
                {
                    return;
                }

                connection.Close();
            }

            public override void Open()
            {
                switch (connection.State)
                {
                    case ConnectionState.Closed:
                        connection.Open();
                        break;
                    case ConnectionState.Connecting:

                        do
                        {
                            Thread.Sleep(5);

                        } while (connection.State == ConnectionState.Connecting);

                        goto default;
                    case ConnectionState.Broken:
                        connection.Close();
                        goto default;
                    default:
                        if (connection.State == ConnectionState.Closed)
                        {
                            connection.Open();
                        }
                        break;
                }

                connectionState = connection.State;
            }

#if NET_NORMAL || NET_CORE
            public override async Task OpenAsync(CancellationToken cancellationToken)
            {
                switch (connection.State)
                {
                    case ConnectionState.Closed:
                        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        break;
                    case ConnectionState.Connecting:
                        do
                        {
                            await Task.Delay(5, cancellationToken).ConfigureAwait(false);

                        } while (State == ConnectionState.Connecting);

                        goto default;
                    case ConnectionState.Broken:
#if NETSTANDARD2_1
                        await connection.CloseAsync()
                            .ConfigureAwait(false);
#else
                        connection.Close();
#endif
                        goto default;
                    default:
                        if (connection.State == ConnectionState.Closed)
                        {
                            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        }
                        break;
                }

                connectionState = connection.State;
            }
#endif

#if NETSTANDARD2_1
            public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default) => connection.ChangeDatabaseAsync(databaseName, cancellationToken);

            protected override ValueTask<System.Data.Common.DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) => connection.BeginTransactionAsync(isolationLevel, cancellationToken);

            public override Task CloseAsync() => connection.CloseAsync();

            public override ValueTask DisposeAsync()
            {
                IsActive = false;

                var task1 = connection
                    .DisposeAsync();

                var task2 = base.DisposeAsync();

                var task = Task.WhenAll(task1.AsTask(), task2.AsTask());

                task.ConfigureAwait(false)
                    .GetAwaiter()
                    .OnCompleted(() =>
                    {
                        if (IsReleased)
                        {
                            return;
                        }

                        IsReleased = true;

                        GC.SuppressFinalize(this);
                    });

                return new ValueTask(task);
            }

#endif

            public bool IsThreadActive => isActiveThread.IsAlive;

            public bool IsAlive => connection.State == ConnectionState.Open;

            public bool IsReleased { private set; get; }

            public DateTime ActiveTime { get; private set; } = DateTime.Now;

            public bool IsActive { get; private set; } = true;

            protected override System.Data.Common.DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => connection.BeginTransaction(isolationLevel);
            protected override System.Data.Common.DbCommand CreateDbCommand() => connection.CreateCommand();

            void IDisposable.Dispose()
            {
                IsActive = false;

                if (useCache)
                {
                    Close();
                }
                else
                {
                    Destroy();
                }
            }

            public void Destroy() => Dispose(true);

            public IDbConnection ReuseConnection()
            {
                connectionState = ConnectionState.Closed;

                isActiveThread = Thread.CurrentThread;

                ActiveTime = DateTime.Now;

                IsActive = true;

                return this;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (IsReleased)
                    {
                        return;
                    }

                    IsReleased = true;

                    connection?.Close();
                    connection?.Dispose();

                    GC.SuppressFinalize(this);

                    base.Dispose(disposing);
                }
            }
        }

        /// <summary>
        /// 默认链接。
        /// </summary>
        private class DefaultConnections : IDispatchConnections
        {
            private bool _clearTimerRun;
            private readonly Timer _clearTimer;
            private readonly ConcurrentDictionary<string, List<IDispatchConnection>> connectionCache = new ConcurrentDictionary<string, List<IDispatchConnection>>();

            public DefaultConnections()
            {
                _clearTimer = new Timer(1000D * 60D);
                _clearTimer.Elapsed += ClearTimerElapsed;
                _clearTimer.Enabled = true;
                _clearTimer.Stop();
                _clearTimerRun = false;
            }

            private void ClearTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                var list = new List<string>();
                var connections = new List<IDispatchConnection>();

                foreach (var kv in connectionCache)
                {
                    kv.Value.RemoveAll(x =>
                    {
                        if (x.IsReleased)
                        {
                            return true;
                        }

                        if (x.IsAlive || x.IsThreadActive)
                        {
                            return false;
                        }

                        connections.Add(x);

                        return true;
                    });

                    if (kv.Value.Count == 0)
                    {
                        list.Add(kv.Key);
                    }
                }

                connections.ForEach(x => x.Destroy());

                list.ForEach(key =>
                {
                    if (connectionCache.TryRemove(key, out connections) && connections.Count > 0)
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

            public IDbConnection Create(string connectionString, IDbConnectionFactory adapter, bool useCache = true)
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentException("数据库链接无效!", nameof(connectionString));
                }

                List<IDispatchConnection> connections = connectionCache.GetOrAdd(connectionString, _ => new List<IDispatchConnection>());

                if (useCache && connections.Count > 0)
                {
                    lock (connections)
                    {
                        foreach (var item in connections)
                        {
                            if (item.IsAlive && !item.IsActive)
                            {
                                return item.ReuseConnection();
                            }
                        }
                    }
                }

                IDispatchConnection connection;

                if (adapter.MaxPoolSize == connections.Count && connections.RemoveAll(x => x.IsReleased) == 0)
                {
                    lock (connections)
                    {
                        connection = connections //? 线程已关闭的。
                             .FirstOrDefault(x => !x.IsThreadActive) ?? connections
                             .Where(x => !x.IsActive)
                             .OrderBy(x => x.ActiveTime) //? 移除最长时间不活跃的链接。
                             .FirstOrDefault() ?? throw new DException($"链接数超限(最大连接数：{adapter.MaxPoolSize})!");

                        return connection.ReuseConnection();
                    }
                }

                var conn = adapter.Create(connectionString);

                if (conn is System.Data.Common.DbConnection dbConnection)
                {
                    connection = new DispatchConnection(dbConnection, adapter.ConnectionHeartbeat, useCache);
                }
                else
                {
                    connection = new DbConnection(conn, adapter.ConnectionHeartbeat, useCache);
                }

                lock (connections)
                {
                    connections.Add(connection);
                }

                if (!_clearTimerRun)
                {
                    _clearTimer.Start();
                    _clearTimerRun = true;
                }

                return connection;
            }
        }

        /// <summary>
        /// 获取数据库连接。
        /// </summary>
        /// <param name="connectionString">链接字符串。</param>
        /// <param name="adapter">数据库适配器。</param>
        /// <param name="useCache">使用缓存。</param>
        /// <returns></returns>
        public IDbConnection GetConnection(string connectionString, IDbConnectionFactory adapter, bool useCache = true) => _connections.Create(connectionString, adapter, useCache);
    }
}
