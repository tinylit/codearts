using CodeArts.Db.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
#if NET40_OR_GREATER
using System.Runtime.Remoting;
#endif
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
using Transaction = System.Transactions.Transaction;

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

        private class DispatchConnection : IDispatchConnection
        {
            private readonly IDbConnection connection; //数据库连接
            private readonly double connectionHeartbeat; //心跳
            private readonly bool useCache;
            private ConnectionState connectionState = ConnectionState.Closed;
            private Thread isActiveThread = Thread.CurrentThread;
            public DispatchConnection(IDbConnection connection, double connectionHeartbeat, bool useCache)
            {
                this.connection = connection;
                this.connectionHeartbeat = connectionHeartbeat;
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

                if (useCache && connectionHeartbeat > 0D && DateTime.Now <= ActiveTime.AddMinutes(connectionHeartbeat))
                {
                    return;
                }

                if (useCache)
                {
                    connection.Close();
                }
                else
                {
                    var current = Transaction.Current;

                    if (current is null)
                    {
                        connection.Close();
                    }
                    else
                    {
                        current.TransactionCompleted += (sender, e) =>
                        {
                            if (connection.State != ConnectionState.Closed)
                            {
                                connection.Close();
                            }
                        };
                    }
                }
            }

            public IDbCommand CreateCommand() => new DbCommand(connection.CreateCommand(), useCache);

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
                    var current = Transaction.Current;

                    if (current is null)
                    {
                        Dispose(true);
                    }
                    else
                    {
                        current.TransactionCompleted += (sender, e) =>
                        {
                            Dispose(true);
                        };
                    }
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
                    if (connection?.State != ConnectionState.Closed)
                    {
                        connection?.Close();
                    }

                    connection?.Dispose();

                    if (IsReleased)
                    {
                        return;
                    }

                    IsReleased = true;

                    GC.SuppressFinalize(this);
                }
            }

            private class DbCommand : IDbCommand
            {
                private readonly IDbCommand command;
                private readonly bool useCache;

                public DbCommand(IDbCommand command, bool useCache)
                {
                    this.command = command;
                    this.useCache = useCache;
                }

                public string CommandText { get => command.CommandText; set => command.CommandText = value; }
                public int CommandTimeout { get => command.CommandTimeout; set => command.CommandTimeout = value; }
                public CommandType CommandType { get => command.CommandType; set => command.CommandType = value; }
                public IDbConnection Connection { get => command.Connection; set => command.Connection = value; }

                public IDataParameterCollection Parameters => command.Parameters;

                public IDbTransaction Transaction { get => command.Transaction; set => command.Transaction = value; }
                public UpdateRowSource UpdatedRowSource { get => command.UpdatedRowSource; set => command.UpdatedRowSource = value; }

                public void Cancel() => command.Cancel();

                public IDbDataParameter CreateParameter() => command.CreateParameter();

                public void Dispose() => command.Dispose();

                public int ExecuteNonQuery() => command.ExecuteNonQuery();

                public IDataReader ExecuteReader() => command.ExecuteReader();

                public IDataReader ExecuteReader(CommandBehavior behavior)
                {
                    if (useCache)
                    {
                        return command.ExecuteReader(behavior & ~CommandBehavior.CloseConnection);
                    }

                    if ((behavior & CommandBehavior.CloseConnection) == CommandBehavior.Default)
                    {
                        return command.ExecuteReader(behavior);
                    }

                    var current = System.Transactions.Transaction.Current;

                    if (current is null)
                    {
                        return command.ExecuteReader(behavior);
                    }
                    else
                    {
                        var connection = command.Connection;

                        current.TransactionCompleted += (sender, e) =>
                        {
                            if (connection.State != ConnectionState.Closed)
                            {
                                connection.Close();
                            }
                        };
                    }

                    return command.ExecuteReader(behavior & ~CommandBehavior.CloseConnection);
                }

                public object ExecuteScalar() => command.ExecuteScalar();

                public void Prepare() => command.Prepare();
            }
        }

        private class DispatchDbConnection : System.Data.Common.DbConnection, IDispatchConnection
        {
            private readonly System.Data.Common.DbConnection connection;

            private readonly double connectionHeartbeat; //心跳
            private readonly bool useCache;
            private ConnectionState connectionState = ConnectionState.Closed;
            private Thread isActiveThread = Thread.CurrentThread;

            public DispatchDbConnection(System.Data.Common.DbConnection connection, double connectionHeartbeat, bool useCache)
            {
                this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
                this.connectionHeartbeat = connectionHeartbeat;
                this.useCache = useCache;
            }

            public override string ConnectionString { get => connection.ConnectionString; set => connection.ConnectionString = value; }

            public override string Database => connection.Database;

            public override string DataSource => connection.DataSource;

            public override string ServerVersion => connection.ServerVersion;

            public override int ConnectionTimeout => connection.ConnectionTimeout;

#if NET40_OR_GREATER
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
            public override void EnlistTransaction(Transaction transaction) => connection.EnlistTransaction(transaction);

            public override void Close()
            {
                connectionState = ConnectionState.Closed;

                if (connection.State == ConnectionState.Closed)
                {
                    return;
                }

                if (useCache && connectionHeartbeat > 0D && DateTime.Now <= ActiveTime.AddMinutes(connectionHeartbeat))
                {
                    return;
                }

                if (useCache)
                {
                    connection.Close();
                }
                else
                {
                    var current = Transaction.Current;

                    if (current is null)
                    {
                        connection.Close();
                    }
                    else
                    {
                        current.TransactionCompleted += (sender, e) =>
                        {
                            if (connection.State != ConnectionState.Closed)
                            {
                                connection.Close();
                            }
                        };
                    }
                }
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

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
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
#if NETSTANDARD2_1_OR_GREATER
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

#if NETSTANDARD2_1_OR_GREATER
            public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default) => connection.ChangeDatabaseAsync(databaseName, cancellationToken);

            protected override ValueTask<System.Data.Common.DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) => connection.BeginTransactionAsync(isolationLevel, cancellationToken);

            public override Task CloseAsync()
            {
                connectionState = ConnectionState.Closed;

                if (connection.State == ConnectionState.Closed)
                {
                    return Task.CompletedTask;
                }

                if (useCache && connectionHeartbeat > 0D && DateTime.Now <= ActiveTime.AddMinutes(connectionHeartbeat))
                {
                    return Task.CompletedTask;
                }

                if (useCache)
                {
                    return connection.CloseAsync();
                }

                var current = Transaction.Current;

                if (current is null)
                {
                    return connection.CloseAsync();
                }

                current.TransactionCompleted += (sender, e) =>
                {
                    if (connection.State != ConnectionState.Closed)
                    {
                        connection.Close();
                    }
                };

                return Task.CompletedTask;
            }

            public override ValueTask DisposeAsync()
            {
                IsActive = false;

                if (useCache)
                {
                    return new ValueTask(CloseAsync());
                }

                var current = Transaction.Current;

                if (current is null)
                {
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

                current.TransactionCompleted += (sender, e) =>
                {
                    Dispose(true);
                };

                return new ValueTask(Task.CompletedTask);
            }

#endif

            public bool IsThreadActive => isActiveThread.IsAlive;

            public bool IsAlive => connection.State == ConnectionState.Open;

            public bool IsReleased { private set; get; }

            public DateTime ActiveTime { get; private set; } = DateTime.Now;

            public bool IsActive { get; private set; } = true;

            protected override System.Data.Common.DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => connection.BeginTransaction(isolationLevel);
            protected override System.Data.Common.DbCommand CreateDbCommand() => new DbCommand(connection.CreateCommand(), useCache);

            void IDisposable.Dispose()
            {
                IsActive = false;

                if (useCache)
                {
                    Close();
                }
                else
                {
                    var current = Transaction.Current;

                    if (current is null)
                    {
                        Dispose(true);
                    }
                    else
                    {
                        current.TransactionCompleted += (sender, e) =>
                        {
                            Dispose(true);
                        };
                    }
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
                    if (connection?.State != ConnectionState.Closed)
                    {
                        connection?.Close();
                    }

                    connection?.Dispose();

                    if (IsReleased)
                    {
                        return;
                    }

                    IsReleased = true;

                    GC.SuppressFinalize(this);

                    base.Dispose(disposing);
                }
            }

            private class DbCommand : System.Data.Common.DbCommand, IDbCommand
            {
                private readonly System.Data.Common.DbCommand command;
                private readonly bool useCache;

                public DbCommand(System.Data.Common.DbCommand command, bool useCache)
                {
                    this.command = command;
                    this.useCache = useCache;
                }

                public override string CommandText { get => command.CommandText; set => command.CommandText = value; }
                public override int CommandTimeout { get => command.CommandTimeout; set => command.CommandTimeout = value; }
                public override CommandType CommandType { get => command.CommandType; set => command.CommandType = value; }
                public override bool DesignTimeVisible { get => command.DesignTimeVisible; set => command.DesignTimeVisible = value; }
                public override UpdateRowSource UpdatedRowSource { get => command.UpdatedRowSource; set => command.UpdatedRowSource = value; }
                protected override System.Data.Common.DbConnection DbConnection { get => command.Connection; set => command.Connection = value; }
                protected override System.Data.Common.DbParameterCollection DbParameterCollection => command.Parameters;
                protected override System.Data.Common.DbTransaction DbTransaction { get => command.Transaction; set => command.Transaction = value; }
                public override void Cancel() => command.Cancel();
                public override int ExecuteNonQuery() => command.ExecuteNonQuery();
                public override object ExecuteScalar() => command.ExecuteScalar();
                public override void Prepare() => command.Prepare();
                public override object InitializeLifetimeService() => command.InitializeLifetimeService();
                protected override System.Data.Common.DbParameter CreateDbParameter() => command.CreateParameter();
                protected override System.Data.Common.DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
                {
                    if (useCache)
                    {
                        return command.ExecuteReader(behavior & ~CommandBehavior.CloseConnection);
                    }

                    if ((behavior & CommandBehavior.CloseConnection) == CommandBehavior.Default)
                    {
                        return command.ExecuteReader(behavior);
                    }

                    var current = System.Transactions.Transaction.Current;

                    if (current is null)
                    {
                        return command.ExecuteReader(behavior);
                    }
                    else
                    {
                        var connection = command.Connection;

                        current.TransactionCompleted += (sender, e) =>
                        {
                            if (connection.State != ConnectionState.Closed)
                            {
                                connection.Close();
                            }
                        };
                    }

                    return command.ExecuteReader(behavior & ~CommandBehavior.CloseConnection);
                }

#if NET451_OR_GREATER || NETSTANDARD2_0_OR_GREATER
                protected override Task<System.Data.Common.DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
                {
                    if (useCache)
                    {
                        return command.ExecuteReaderAsync(behavior & ~CommandBehavior.CloseConnection, cancellationToken);
                    }

                    if ((behavior & CommandBehavior.CloseConnection) == CommandBehavior.Default || System.Transactions.Transaction.Current is null)
                    {
                        return command.ExecuteReaderAsync(behavior, cancellationToken);
                    }

                    var current = System.Transactions.Transaction.Current;

                    if (current is null)
                    {
                        return command.ExecuteReaderAsync(behavior, cancellationToken);
                    }
                    else
                    {
                        var connection = command.Connection;

                        current.TransactionCompleted += (sender, e) =>
                        {
                            if (connection.State != ConnectionState.Closed)
                            {
                                connection.Close();
                            }
                        };
                    }

                    return command.ExecuteReaderAsync(behavior & ~CommandBehavior.CloseConnection, cancellationToken);
                }

                public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => command.ExecuteNonQueryAsync(cancellationToken);

                public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) => command.ExecuteScalarAsync(cancellationToken);
#endif
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
                    connection = new DispatchDbConnection(dbConnection, adapter.ConnectionHeartbeat, useCache);
                }
                else
                {
                    connection = new DispatchConnection(conn, adapter.ConnectionHeartbeat, useCache);
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
        public IDbConnection GetConnection(string connectionString, IDbConnectionFactory adapter, bool useCache = true)
        {
            if (!useCache //? 不复用链接。
                || Transaction.Current is null) //? 不在事务范围中。
            {
                return _connections.Create(connectionString, adapter, useCache);
            }

            return TransactionConnections.GetConnection(connectionString, adapter);
        }
    }
}
