﻿using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace CodeArts.Db
{
    /// <summary>
    /// MySQL 适配器。
    /// </summary>
    public class MySqlFactory : IDbConnectionFactory
    {
        /// <summary>
        /// MySql。
        /// </summary>
        public const string Name = "MySql";

        /// <summary> 
        /// 适配器名称。 
        /// </summary>
        public string ProviderName => Name;

        /// <summary>
        /// 线程池数量。
        /// </summary>
        public int MaxPoolSize { set; get; } = 100;

        /// <summary>
        /// 心跳。
        /// </summary>
        public virtual double ConnectionHeartbeat { get; set; } = 5D;

        /// <summary>
        /// typeof(<see cref="MySqlConnection"/>)
        /// </summary>
        public Type DbConnectionType => typeof(MySqlConnection);

        /// <summary>
        /// 创建数据库连接。
        /// </summary>
        /// <param name="connectionString">数据库连接字符串。</param>
        /// <returns></returns>
        public virtual IDbConnection Create(string connectionString) => new MySqlConnection(connectionString);
    }
}
