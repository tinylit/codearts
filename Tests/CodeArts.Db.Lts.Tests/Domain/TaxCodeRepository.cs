using CodeArts.Db;
using CodeArts.Db.Lts;
using CodeArts.Db.Lts.Tests;
using UnitTest.Domain.Entities;

namespace UnitTest.Domain
{
    /// <summary>
    /// 发票仓库。
    /// </summary>
    public class TaxCodeRepository : DbRepository<TaxCode>
    {
        protected override IReadOnlyConnectionConfig GetDbConfig()
        {
            return new ConnectionConfig
            {
                Name = "yep.v3.invoice",
                ProviderName = "MySql",
                ConnectionString = string.Format("server={0};port=3306;user={2};password={3};database={1};"
                , MySqlConsts.Domain
                , MySqlConsts.Database
                , MySqlConsts.User
                , MySqlConsts.Password)//? 数据库链接
            };
        }
    }
}
