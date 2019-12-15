using System;
using System.Data;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据库(Close和Dispose接口虚拟，通过接口调用时不会真正关闭或释放，只有通过类调用才会真实的执行)
    /// </summary>
    public class DbConnection : IDbConnection
    {
        private readonly IDbConnection _connection; //数据库连接

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connection">数据库链接</param>
        public DbConnection(IDbConnection connection) => _connection = connection;

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
        public virtual void Open()
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        void IDbConnection.Close() { }

        /// <summary>
        /// 关闭链接
        /// </summary>
        public virtual void Close() => _connection.Close();

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

                GC.SuppressFinalize(this);
            }
        }
    }
}
