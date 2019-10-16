using System;
using System.Data;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 数据库
    /// </summary>
    public abstract class DbConnection : IDbConnection
    {
        //事务中，当前还有多少次连接
        private readonly IDbConnection _connection; //数据库连接

        public DbConnection(IDbConnection connection)
        {
            _connection = connection;
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
            if (_connection.State == ConnectionState.Closed)
            {
                Open();
            }

            return _connection.BeginTransaction();
        }

        /// <summary>
        /// 创建事务
        /// </summary>
        /// <param name="il">隔离等级</param>
        /// <returns></returns>
        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            if (_connection.State == ConnectionState.Closed)
            {
                Open();
            }

            return _connection.BeginTransaction(il);
        }

        /// <summary>
        /// 修改数据库
        /// </summary>
        /// <param name="databaseName">数据库名称</param>
        public void ChangeDatabase(string databaseName)
        {
            if (State == ConnectionState.Closed)
            {
                Open();
            }

            _connection.ChangeDatabase(databaseName);
        }

        /// <summary>
        /// 打开链接
        /// </summary>
        public virtual void Open() => _connection.Open();

        /// <summary>
        /// 关闭链接
        /// </summary>
        public virtual void Close() => _connection.Close();

        /// <summary>
        /// 创建命令
        /// </summary>
        /// <returns></returns>
        public IDbCommand CreateCommand()
        {
            if (_connection.State == ConnectionState.Closed)
            {
                Open();
            }

            return _connection.CreateCommand();
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection?.Close();
                _connection?.Dispose();
            }
        }
    }
}
