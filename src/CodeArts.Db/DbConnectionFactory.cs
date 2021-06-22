using System;
using System.Data;
using System.Data.Common;

namespace CodeArts.Db
{
    /// <summary>
    /// 数据库连接。
    /// </summary>
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private bool initDbConnectionType = true;

        private readonly DbProviderFactory dbProvider;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="providerName">供应商名称。</param>
        /// <param name="dbProviderFactory">供应商工厂。</param>
        public DbConnectionFactory(string providerName, DbProviderFactory dbProviderFactory) : this(providerName, dbProviderFactory, 5D) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="providerName">供应商名称。</param>
        /// <param name="dbProviderFactory">供应商工厂。</param>
        /// <param name="connectionHeartbeat">连接心跳。</param>
        public DbConnectionFactory(string providerName, DbProviderFactory dbProviderFactory, double connectionHeartbeat)
        {
            ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
            dbProvider = dbProviderFactory ?? throw new ArgumentNullException(nameof(dbProviderFactory));
            ConnectionHeartbeat = connectionHeartbeat;
        }

        /// <summary>
        /// 供应商名称。
        /// </summary>
        public string ProviderName { get; }

        /// <summary>
        /// 线程池数量。
        /// </summary>
        public int MaxPoolSize { set; get; } = 100;

        /// <summary>
        /// 连接心跳（默认:5D）。
        /// </summary>
        public double ConnectionHeartbeat { get; }

        private Type connectionType;

        /// <summary>
        /// 连接类型。
        /// </summary>
        public Type DbConnectionType
        {
            get
            {
                if (connectionType is null)
                {
                    try
                    {
                        var connection = dbProvider.CreateConnection();

                        connectionType = connection.GetType();

                        connection.Dispose();
                    }
                    catch
                    {
                        if (connectionType is null)
                        {
                            connectionType = typeof(DbConnection);
                        }
                    }
                }

                return connectionType;
            }
        }

        /// <summary>
        /// 创建数据库连接。
        /// </summary>
        /// <param name="connectionString">数据库连接字符串。</param>
        /// <returns></returns>
        public IDbConnection Create(string connectionString)
        {
            var connection = dbProvider.CreateConnection();

            connection.ConnectionString = connectionString;

            if (connectionType is null)
            {
                connectionType = connection.GetType();
            }

            return connection;
        }
    }
}
