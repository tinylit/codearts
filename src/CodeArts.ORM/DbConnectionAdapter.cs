using System;
using System.Data;
using System.Data.Common;

namespace CodeArts.ORM
{
    /// <summary>
    /// 链接适配器
    /// </summary>
    public class DbConnectionAdapter : IDbConnectionAdapter
    {
        private readonly DbProviderFactory dbProvider;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="providerName">供应商名称</param>
        /// <param name="settings">SQL矫正配置</param>
        /// <param name="dbProviderFactory">供应商工厂</param>
        public DbConnectionAdapter(string providerName, ISQLCorrectSettings settings, DbProviderFactory dbProviderFactory) : this(providerName, settings, dbProviderFactory, 5D) { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="providerName">供应商名称</param>
        /// <param name="settings">SQL矫正配置</param>
        /// <param name="dbProviderFactory">供应商工厂</param>
        /// <param name="connectionHeartbeat">连接心跳</param>
        public DbConnectionAdapter(string providerName, ISQLCorrectSettings settings, DbProviderFactory dbProviderFactory, double connectionHeartbeat)
        {
            ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            dbProvider = dbProviderFactory ?? throw new ArgumentNullException(nameof(dbProviderFactory));
            ConnectionHeartbeat = connectionHeartbeat;
        }

        /// <summary>
        /// 供应商名称
        /// </summary>
        public string ProviderName { get; }

        /// <summary>
        /// 连接心跳（默认:5D）
        /// </summary>
        public double ConnectionHeartbeat { get; }

        /// <summary>
        /// SQL 矫正配置
        /// </summary>
        public ISQLCorrectSettings Settings { get; }

        /// <summary>
        /// 创建数据库连接
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <returns></returns>
        public IDbConnection Create(string connectionString)
        {
            var connection = dbProvider.CreateConnection();

            connection.ConnectionString = connectionString;

            return connection;
        }
    }
}
