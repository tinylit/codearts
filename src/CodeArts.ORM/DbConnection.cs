using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据库(Close和Dispose接口虚拟，通过接口调用时不会真正关闭或释放，只有通过类调用才会真实的执行)
    /// </summary>
    public class DbConnection : IDbConnection
    {
        /// <summary>
        /// 数据读取器。
        /// </summary>
        private class DbReader : IDataReader
        {
            private bool disposed = false;
            private readonly DbConnection connection;
            private readonly DbCommand command;
            private readonly IDataReader reader;
            private readonly bool isClosedConnection;

            public DbReader(DbConnection connection, DbCommand command, IDataReader reader, bool isClosedConnection)
            {
                this.connection = connection;
                this.command = command;
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
                reader.Dispose();

                if (isClosedConnection)
                {
                    connection.Close();
                }

                if (disposed)
                {
                    return;
                }

                disposed = true;
                command.Decrement();
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

        /// <summary>
        /// 命令。
        /// </summary>
        private class DbCommand : IDbCommand
        {
            private bool isAlive = false;
            private int refCount = 0;
            private volatile int commandType = 0;
            private readonly IDbCommand command;
            private readonly DbConnection connection;

            public DbCommand(DbConnection connection, IDbCommand command)
            {
                this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
                this.command = command ?? throw new ArgumentNullException(nameof(command));
            }

            /// <summary>
            /// 连接。
            /// </summary>
            public IDbConnection Connection
            {
                get => command.Connection;
                set
                {
                    connection.Remove(this);

                    if (value is DbConnection db)
                    {
                        db.Add(this);

                        command.Connection = db._connection;
                    }
                    else
                    {
                        command.Connection = value;
                    }
                }
            }

            /// <summary>
            /// 事务。
            /// </summary>
            public IDbTransaction Transaction { get => command.Transaction; set => command.Transaction = value; }

            /// <summary>
            /// 命令。
            /// </summary>
            public string CommandText { get => command.CommandText; set => command.CommandText = value; }

            /// <summary>
            /// 命令超时时间。
            /// </summary>
            public int CommandTimeout { get => command.CommandTimeout; set => command.CommandTimeout = value; }

            /// <summary>
            /// 命令类型。
            /// </summary>
            public CommandType CommandType { get => command.CommandType; set => command.CommandType = value; }

            /// <summary>
            /// 参数。
            /// </summary>
            public IDataParameterCollection Parameters => command.Parameters;
            /// <summary>
            /// 获取或设置命令结果在由 System.Data.Common.DbDataAdapter 的 System.Data.IDataAdapter.Update(System.Data.DataSet) 方法使用时应用于 System.Data.DataRow 的方式。
            /// </summary>
            public UpdateRowSource UpdatedRowSource { get => command.UpdatedRowSource; set => command.UpdatedRowSource = value; }

            /// <summary>
            /// 取消指令。
            /// </summary>
            public void Cancel() => command.Cancel();

            /// <summary>
            /// 创建参数。
            /// </summary>
            /// <returns></returns>
            public IDbDataParameter CreateParameter() => command.CreateParameter();

            /// <summary>
            /// 释放资源。
            /// </summary>
            public void Dispose()
            {
                connection.Remove(this);

                command.Dispose();
            }

            public IDataReader Add(IDataReader reader, bool isClosedConnection = false)
            {
                Interlocked.Increment(ref refCount);

                return new DbReader(connection, this, reader, isClosedConnection);
            }

            public void Decrement()
            {
                Interlocked.Decrement(ref refCount);
            }

            /// <summary>
            /// 执行，返回影响行。
            /// </summary>
            /// <returns></returns>
            public int ExecuteNonQuery()
            {
                commandType = 1;

                return command.ExecuteNonQuery();
            }

            /// <summary>
            /// 存活的。
            /// </summary>
            public bool IsAlive
            {
                get
                {
                    if (isAlive || commandType == 0 || refCount > 0)
                    {
                        return true;
                    }

                    Interlocked.CompareExchange(ref refCount, 0, 0);

                    return refCount > 0;
                }
            }

            /// <summary>
            /// 执行并生成读取器。
            /// </summary>
            /// <returns></returns>
            public IDataReader ExecuteReader() => Add(command.ExecuteReader());

            /// <summary>
            /// 执行并生成读取器。
            /// </summary>
            /// <returns></returns>
            public IDataReader ExecuteReader(CommandBehavior behavior)
            {
                bool isClosedConnection = false;

                if ((behavior & CommandBehavior.CloseConnection) == CommandBehavior.CloseConnection)
                {
                    isClosedConnection = true;
                    behavior &= ~CommandBehavior.CloseConnection;
                }

                return Add(command.ExecuteReader(behavior), isClosedConnection);
            }

            /// <summary>
            /// 执行返回首行首列。
            /// </summary>
            /// <returns></returns>
            public object ExecuteScalar()
            {
                isAlive = true;

                try
                {
                    return command.ExecuteScalar();
                }
                finally
                {
                    isAlive = false;
                }
            }

            /// <summary>
            /// 准备就绪。
            /// </summary>
            public void Prepare() => command.Prepare();
        }

        /// <summary>
        /// 事务。
        /// </summary>
        private class DbTransaction : IDbTransaction
        {
            private bool completed = false;
            private readonly DbConnection connection;
            private readonly IDbTransaction transaction;

            /// <summary>
            /// 构造函数。
            /// </summary>
            public DbTransaction(DbConnection connection, IDbTransaction transaction)
            {
                this.connection = connection;
                this.transaction = transaction;
            }

            public IDbConnection Connection => transaction.Connection;

            public IsolationLevel IsolationLevel => transaction.IsolationLevel;

            public void Commit()
            {
                transaction.Commit();

                if (completed)
                {
                    return;
                }

                completed = true;

                connection.DecrementTransaction();
            }

            public void Dispose()
            {
                transaction.Dispose();

                GC.SuppressFinalize(this);

                if (completed)
                {
                    return;
                }

                completed = true;

                connection.DecrementTransaction();
            }

            public void Rollback()
            {
                transaction.Rollback();

                if (completed)
                {
                    return;
                }

                completed = true;

                connection.DecrementTransaction();
            }
        }

        private DateTime lastUseTime;
        private DateTime lastActiveTime;
        private int refCount = 0;
        private int refUseCount = 1;
        private readonly IDbConnection _connection; //数据库连接
        private readonly double? connectionHeartbeat; //心跳
        private readonly List<DbCommand> commands = new List<DbCommand>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="connection">数据库链接</param>
        public DbConnection(IDbConnection connection) => _connection = connection;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="connection">数据库链接</param>
        /// <param name="connectionHeartbeat">链接心跳</param>
        public DbConnection(IDbConnection connection, double connectionHeartbeat) : this(connection)
        {
            lastUseTime = lastActiveTime = DateTime.Now;
            this.connectionHeartbeat = new double?(connectionHeartbeat);
        }

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
        public IDbTransaction BeginTransaction()
        {
            Interlocked.Increment(ref refCount);

            return new DbTransaction(this, _connection.BeginTransaction());
        }

        /// <summary>
        /// 创建事务
        /// </summary>
        /// <param name="il">隔离等级</param>
        /// <returns></returns>
        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            Interlocked.Increment(ref refCount);

            return new DbTransaction(this, _connection.BeginTransaction(il));
        }

        /// <summary>
        /// 修改数据库
        /// </summary>
        /// <param name="databaseName">数据库名称</param>
        public void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

        /// <summary>
        /// 打开链接
        /// </summary>
        public virtual void Open()
        {
            switch (State)
            {
                case ConnectionState.Closed:
                    try
                    {
                        _connection.Open();
                    }
                    catch (Exception e)
                    {

                        throw e;
                    }
                    break;
                case ConnectionState.Connecting:

                    do
                    {
                        Thread.Sleep(5);

                    } while (State == ConnectionState.Connecting);

                    break;
                case ConnectionState.Broken:
                    _connection.Close();
                    _connection.Open();
                    break;
            }
        }

        void IDbConnection.Close() => Close();

        private void DecrementTransaction()
        {
            Interlocked.Decrement(ref refCount);
        }

        private void Add(DbCommand command)
        {
            if (!commands.Contains(command))
            {
                commands.Add(command);
            }
        }

        private void Remove(DbCommand command)
        {
            commands.Remove(command);
        }

        /// <summary>
        /// 关闭链接
        /// </summary>
        public virtual void Close()
        {
            if (State == ConnectionState.Closed)
            {
                return;
            }

            if (connectionHeartbeat.HasValue && DateTime.Now <= lastActiveTime.AddMinutes(connectionHeartbeat.Value))
            {
                return;
            }

            _connection.Close();
        }

        /// <summary>
        /// 创建命令
        /// </summary>
        /// <returns></returns>
        public IDbCommand CreateCommand()
        {
            var command = new DbCommand(this, _connection.CreateCommand());

            commands.Add(command);

            lastActiveTime = DateTime.Now;

            return command;
        }

        /// <summary>
        /// 是否存活。
        /// </summary>
        public bool IsAlive
        {
            get
            {
                if (connectionHeartbeat.HasValue)
                {
                    return State == ConnectionState.Open && DateTime.Now <= lastActiveTime.AddMinutes(connectionHeartbeat.Value);
                }

                return State == ConnectionState.Open;
            }
        }

        /// <summary>
        /// 是否闲置。
        /// </summary>
        public bool IsIdle
        {
            get
            {
                if (refCount > 0 || lastUseTime.AddMilliseconds(520D) > DateTime.Now)
                {
                    return false;
                }

                if (commands.Any(x => x.IsAlive))
                {
                    return false;
                }

                return Interlocked.CompareExchange(ref refCount, 0, 0) == 0;
            }
        }

        /// <summary>
        /// 释放器不释放
        /// </summary>
        void IDisposable.Dispose()
        {
            Interlocked.Increment(ref refUseCount);
        }

        /// <summary>
        /// 复用。
        /// </summary>
        /// <returns></returns>
        public IDbConnection ReuseConnection()
        {
            Interlocked.Increment(ref refUseCount);

            lastUseTime = DateTime.Now;

            return this;
        }

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
            if (disposing && Interlocked.CompareExchange(ref refUseCount, 0, 0) == 0)
            {
                _connection?.Close();
                _connection?.Dispose();

                GC.SuppressFinalize(this);
            }
        }
    }
}
