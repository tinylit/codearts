using CodeArts.Db;
using CodeArts.Db.EntityFramework;
using UnitTest.Domain.Entities;

namespace UnitTest.Domain
{
    /// <summary>
    /// 发票仓库。
    /// </summary>
    public class TaxCodeRepository : Repository<TaxCode>
    {
        protected override IReadOnlyConnectionConfig GetDbConfig()
        {
            return new ConnectionConfig
            {
                Name = "yep.v3.invoice",
                ProviderName = "MySql",
                ConnectionString = "server=127.0.0.1;port=3306;user=root;password=Password12!;database=mysql;"
            };
        }
    }
}
