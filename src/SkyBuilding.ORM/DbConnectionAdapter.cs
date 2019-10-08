using System;
using System.Data;
using System.Data.Common;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 链接适配器
    /// </summary>
    public class DbConnectionAdapter : IDbConnectionAdapter
    {
        private readonly DbProviderFactory dbProvider;
        public DbConnectionAdapter(string providerName, ISQLCorrectSettings settings, DbProviderFactory dbProviderFactory) : this(providerName, settings, dbProviderFactory, 5D) { }

        public DbConnectionAdapter(string providerName, ISQLCorrectSettings settings, DbProviderFactory dbProviderFactory, double connectionHeartbeat)
        {
            ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            dbProvider = dbProviderFactory ?? throw new ArgumentNullException(nameof(dbProviderFactory));
            ConnectionHeartbeat = connectionHeartbeat;
        }

        public string ProviderName { get; }

        public double ConnectionHeartbeat { get; }

        public ISQLCorrectSettings Settings { get; }

        public IDbConnection Create(string connectionString)
        {
            var connection = dbProvider.CreateConnection();

            connection.ConnectionString = connectionString;

            return connection;
        }
    }
}
