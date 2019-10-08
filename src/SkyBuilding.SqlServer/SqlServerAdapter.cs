using SkyBuilding.ORM;
using SkyBuilding.ORM.SqlServer;
using System.Data;
using System.Data.SqlClient;

namespace SkyBuilding.SqlServer
{
    public class SqlServerAdapter : IDbConnectionAdapter
    {
        public const string Name = "SqlServer";

        /// <summary> 适配器名称 </summary>
        public string ProviderName => Name;

        public virtual ISQLCorrectSettings Settings => Singleton<SqlServerCorrectSettings>.Instance;

        public virtual double ConnectionHeartbeat => 5D;

        /// <summary> 创建数据库连接 </summary>
        /// <returns></returns>
        public virtual IDbConnection Create(string connectionString) => new SqlConnection(connectionString);
    }
}
