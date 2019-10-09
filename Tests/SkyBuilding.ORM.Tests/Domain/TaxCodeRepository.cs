using SkyBuilding.ORM;
using System;
using System.Collections.Generic;
using System.Text;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace UnitTest.Domain
{
    /// <summary>
    /// 发票仓库
    /// </summary>
    public class TaxCodeRepository : DbRepository<TaxCode>
    {
        protected override ConnectionConfig GetDbConfig()
        {
            return new ConnectionConfig
            {
                Name = "yep.v3.invoice",
                ProviderName = "MySql",
                ConnectionString = ""
            };
        }
    }
}
