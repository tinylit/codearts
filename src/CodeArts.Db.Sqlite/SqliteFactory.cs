#if NETSTANDARD2_0
using Microsoft.Data.Sqlite;
#endif
using System;
using System.Data;

namespace CodeArts.Db
{
    /// <summary>
    /// Sqlite 工厂。
    /// </summary>
    public class SqliteFactory : IDbConnectionFactory
    {
        /// <summary>
        /// Sqlite。
        /// </summary>
        public const string Name = "Sqlite";

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

#if NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// typeof(<see cref="SqliteConnection"/>)
        /// </summary>
        public Type DbConnectionType => typeof(SqliteConnection);
        #else
        /// <summary>
        /// typeof(<see cref="System.Data.SQLite.SQLiteConnection"/>)
        /// </summary>
        public Type DbConnectionType => typeof(System.Data.SQLite.SQLiteConnection);
#endif


        /// <summary>
        /// 创建数据库连接。
        /// </summary>
        /// <param name="connectionString">数据库连接字符串。</param>
        /// <returns></returns>
        public virtual IDbConnection Create(string connectionString)
#if NETSTANDARD2_0_OR_GREATER
            => new SqliteConnection(connectionString);
#else
            => new System.Data.SQLite.SQLiteConnection(connectionString);
#endif
    }
}
