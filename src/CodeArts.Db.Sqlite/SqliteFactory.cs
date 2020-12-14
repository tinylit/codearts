#if NETSTANDARD2_0
using Microsoft.Data.Sqlite;
#endif
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

        /// <summary>
        /// 创建数据库连接。
        /// </summary>
        /// <param name="connectionString">数据库连接字符串。</param>
        /// <returns></returns>
        public virtual IDbConnection Create(string connectionString)
#if NETSTANDARD2_0
            => new SqliteConnection(connectionString);
#else
            => new System.Data.SQLite.SQLiteConnection(connectionString);
#endif
    }
}
