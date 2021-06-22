using System;
using System.Data;
using System.Data.SqlClient;

namespace CodeArts.Db
{
    /// <summary>
    /// SqlServer 适配器。
    /// </summary>
    public class SqlServerFactory : IDbConnectionFactory
    {
        /// <summary>
        /// SqlServer。
        /// </summary>
        public const string Name = "SqlServer";

        /// <summary> 适配器名称。 </summary>
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
        /// typeof(<see cref="SqlConnection"/>)
        /// </summary>
        public Type DbConnectionType => typeof(SqlConnection);

        /// <summary> 创建数据库连接。 </summary>
        /// <returns></returns>
        public virtual IDbConnection Create(string connectionString) => new SqlConnection(connectionString);
    }
}
