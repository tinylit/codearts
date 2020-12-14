using CodeArts.Db.Exceptions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
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
            private class DbReader : IDataReader
            {
                private bool disposed = false;
                private readonly DbConnection connection;
                private readonly IDataReader reader;
                private readonly bool isClosedConnection;

                public DbReader(DbConnection connection, IDataReader reader, bool isClosedConnection)
                {
                    this.connection = connection;
                    this.reader = reader;
                    this.isClosedConnection = isClosedConnection;
                }
                public object this[int i] => reader[i];

                public object this[string name] => reader[name];

                public int Depth => reader.Depth;

                public bool IsClosed => reader.IsClosed;

                public int RecordsAffected => reader.RecordsAffected;

                public int FieldCount => reader.FieldCount;

                public void Close()
                {
                    reader.Close();

                    if (isClosedConnection)
                    {
                        connection.Close();
                    }
                }

                public void Dispose()
                {
                    if (disposed)
                    {
                        return;
                    }

                    disposed = true;

                    reader.Dispose();

                    if (isClosedConnection)
                    {
                        connection.Close();
                    }
                }

                public bool GetBoolean(int i) => reader.GetBoolean(i);

                public byte GetByte(int i) => reader.GetByte(i);

                public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
                public char GetChar(int i) => reader.GetChar(i);

                public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);

                public IDataReader GetData(int i) => reader.GetData(i);

                public string GetDataTypeName(int i) => reader.GetDataTypeName(i);

                public DateTime GetDateTime(int i) => reader.GetDateTime(i);

                public decimal GetDecimal(int i) => reader.GetDecimal(i);

                public double GetDouble(int i) => reader.GetDouble(i);

                public Type GetFieldType(int i) => reader.GetFieldType(i);

                public float GetFloat(int i) => reader.GetFloat(i);

                public Guid GetGuid(int i) => reader.GetGuid(i);

                public short GetInt16(int i) => reader.GetInt16(i);

                public int GetInt32(int i) => reader.GetInt32(i);

                public long GetInt64(int i) => reader.GetInt64(i);

                public string GetName(int i) => reader.GetName(i);

                public int GetOrdinal(string name) => reader.GetOrdinal(name);

                public DataTable GetSchemaTable() => reader.GetSchemaTable();

                public string GetString(int i) => reader.GetString(i);

                public object GetValue(int i) => reader.GetValue(i);

                public int GetValues(object[] values) => reader.GetValues(values);

                public bool IsDBNull(int i) => reader.IsDBNull(i);

                public bool NextResult() => reader.NextResult();

                public bool Read() => reader.Read();
            }

            private class DbCommand : IDbCommand
            {
                private readonly IDbCommand command;
                private readonly DbConnection connection;

                public DbCommand(DbConnection connection, IDbCommand command)
                {
                    this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
                    this.command = command ?? throw new ArgumentNullException(nameof(command));
                }

                public IDbConnection Connection
                {
                    get => command.Connection;
                    set
                    {
                        if (value is DbConnection db)
                        {
                            command.Connection = db.connection;
                        }
                        else
                        {
                            command.Connection = value;
                        }
                    }
                }

                public IDbTransaction Transaction { get => command.Transaction; set => command.Transaction = value; }

                public string CommandText { get => command.CommandText; set => command.CommandText = value; }

                public int CommandTimeout { get => command.CommandTimeout; set => command.CommandTimeout = value; }

                public CommandType CommandType { get => command.CommandType; set => command.CommandType = value; }

                public IDataParameterCollection Parameters => command.Parameters;
                public UpdateRowSource UpdatedRowSource { get => command.UpdatedRowSource; set => command.UpdatedRowSource = value; }

                public void Cancel() => command.Cancel();

                public IDbDataParameter CreateParameter() => command.CreateParameter();

                public void Dispose() => command.Dispose();

                public int ExecuteNonQuery() => command.ExecuteNonQuery();

                public IDataReader ExecuteReader() => command.ExecuteReader();

                public IDataReader ExecuteReader(CommandBehavior behavior)
                {
                    bool isClosedConnection = false;

                    if ((behavior & CommandBehavior.CloseConnection) == CommandBehavior.CloseConnection)
                    {
                        isClosedConnection = true;

                        behavior &= ~CommandBehavior.CloseConnection;
                    }

                    return new DbReader(connection, command.ExecuteReader(behavior), isClosedConnection);
                }

                public object ExecuteScalar() => command.ExecuteScalar();

                public void Prepare() => command.Prepare();
            }

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

                ActiveTime = DateTime.Now;

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

            public IDbCommand CreateCommand() => new DbCommand(this, connection.CreateCommand());

            public bool IsThreadActive => isActiveThread.IsAlive;

            public bool IsAlive => connection.State == ConnectionState.Open;

            public bool IsActive { get; private set; } = true;

            public bool IsReleased { private set; get; }

            public DateTime ActiveTime { get; private set; }

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
                    IsReleased = true;

                    if (IsReleased)
                    {
                        return;
                    }

                    connection?.Close();
                    connection?.Dispose();

                    GC.SuppressFinalize(this);
                }
            }
        }

        private class DispatchConnection : System.Data.Common.DbConnection, IDispatchConnection
        {
            private readonly System.Data.Common.DbConnection connection;

            private class DbReader : System.Data.Common.DbDataReader
            {
                private bool disposed = false;
                private readonly DispatchConnection connection;
                private readonly System.Data.Common.DbDataReader reader;

                public DbReader(DispatchConnection connection, System.Data.Common.DbDataReader reader)
                {
                    this.connection = connection;
                    this.reader = reader;
                }

                public override int Depth => reader.Depth;

                public override int FieldCount => reader.FieldCount;

                public override bool HasRows => reader.HasRows;

                public override bool IsClosed => reader.IsClosed;

                public override int RecordsAffected => reader.RecordsAffected;

                public override object this[string name] => reader[name];

                public override object this[int ordinal] => reader[ordinal];

                public override void Close()
                {
                    reader.Close();

                    connection.Close();
                }

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        if (disposed)
                        {
                            return;
                        }

                        reader.Dispose();

                        connection.Close();

                        disposed = true;
                    }
                }

                public override bool GetBoolean(int i) => reader.GetBoolean(i);

                public override byte GetByte(int i) => reader.GetByte(i);

                public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);

                public override char GetChar(int i) => reader.GetChar(i);

                public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);

                public override string GetDataTypeName(int i) => reader.GetDataTypeName(i);

                public override DateTime GetDateTime(int i) => reader.GetDateTime(i);

                public override decimal GetDecimal(int i) => reader.GetDecimal(i);

                public override double GetDouble(int i) => reader.GetDouble(i);

                public override Type GetFieldType(int i) => reader.GetFieldType(i);

                public override float GetFloat(int i) => reader.GetFloat(i);

                public override Guid GetGuid(int i) => reader.GetGuid(i);

                public override short GetInt16(int i) => reader.GetInt16(i);

                public override int GetInt32(int i) => reader.GetInt32(i);

                public override long GetInt64(int i) => reader.GetInt64(i);

                public override string GetName(int i) => reader.GetName(i);

                public override int GetOrdinal(string name) => reader.GetOrdinal(name);

                public override DataTable GetSchemaTable() => reader.GetSchemaTable();

                public override string GetString(int i) => reader.GetString(i);

                public override object GetValue(int i) => reader.GetValue(i);

                public override int GetValues(object[] values) => reader.GetValues(values);

                public override bool IsDBNull(int i) => reader.IsDBNull(i);

                public override bool NextResult() => reader.NextResult();

                public override bool Read() => reader.Read();

                public override IEnumerator GetEnumerator() => reader.GetEnumerator();
            }

            private class DbCommand : System.Data.Common.DbCommand
            {
                private readonly System.Data.Common.DbCommand command;
                private readonly DispatchConnection connection;

                public DbCommand(DispatchConnection connection, System.Data.Common.DbCommand command)
                {
                    this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
                    this.command = command ?? throw new ArgumentNullException(nameof(command));
                }

                public override string CommandText { get => command.CommandText; set => command.CommandText = value; }
                public override int CommandTimeout { get => command.CommandTimeout; set => command.CommandTimeout = value; }
                public override CommandType CommandType { get => command.CommandType; set => command.CommandType = value; }
                protected override System.Data.Common.DbConnection DbConnection
                {
                    get => command.Connection;
                    set
                    {
                        if (value is DispatchConnection db)
                        {
                            command.Connection = db.connection;
                        }
                        else
                        {
                            command.Connection = value;
                        }
                    }
                }
                protected override System.Data.Common.DbParameterCollection DbParameterCollection => command.Parameters;
                protected override System.Data.Common.DbTransaction DbTransaction { get => command.Transaction; set => command.Transaction = value; }
                public override bool DesignTimeVisible { get => command.DesignTimeVisible; set => command.DesignTimeVisible = value; }
                public override UpdateRowSource UpdatedRowSource { get => command.UpdatedRowSource; set => command.UpdatedRowSource = value; }

                public override object ExecuteScalar() => command.ExecuteScalar();

                public override void Prepare() => command.Prepare();

                public override void Cancel() => command.Cancel();

                protected override System.Data.Common.DbParameter CreateDbParameter() => command.CreateParameter();

                protected override System.Data.Common.DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
                {
                    if ((behavior & CommandBehavior.CloseConnection) == CommandBehavior.CloseConnection)
                    {
                        return new DbReader(connection, command.ExecuteReader(behavior & ~CommandBehavior.CloseConnection));
                    }

                    return command.ExecuteReader(behavior);
                }

                public override int ExecuteNonQuery() => command.ExecuteNonQuery();

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        command.Dispose();
                    }

                    base.Dispose(disposing);
                }
            }

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

            public override ConnectionState State => connectionState == ConnectionState.Closed ? connectionState : connection.State;

            public override void ChangeDatabase(string databaseName) => connection.ChangeDatabase(databaseName);

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

#if NET_NORMAL
            public override async Task OpenAsync(CancellationToken cancellationToken)
            {
                await connection.OpenAsync(cancellationToken);

                connectionState = connection.State;
            }
#endif

            public bool IsThreadActive => isActiveThread.IsAlive;

            public bool IsAlive => connection.State == ConnectionState.Open;

            public bool IsReleased { private set; get; }

            public DateTime ActiveTime { get; private set; }

            public bool IsActive { get; private set; } = true;

            protected override System.Data.Common.DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => connection.BeginTransaction(isolationLevel);
            protected override System.Data.Common.DbCommand CreateDbCommand() => new DbCommand(this, connection.CreateCommand());

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
                    IsReleased = true;

                    if (IsReleased)
                    {
                        return;
                    }

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
                    if (connections.Any(x => !x.IsThreadActive || !x.IsActive))
                    {
                        lock (connections)
                        {
                            connection = connections //? 线程已关闭的。
                                 .FirstOrDefault(x => !x.IsThreadActive) ?? connections
                                 .Where(x => !x.IsActive)
                                 .OrderBy(x => x.ActiveTime) //? 移除最长时间不活跃的链接。
                                 .FirstOrDefault();

                            if (connection is null)
                            {
                                throw new DException($"链接数超限(最大连接数：{adapter.MaxPoolSize})!");
                            }

                            connections.Remove(connection);
                        }

                        connection.Destroy();
                    }
                    else
                    {
                        throw new DException($"链接数超限(最大连接数：{adapter.MaxPoolSize})!");
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
