using SkyBuilding.ORM;
using System;

namespace UnitTest.Serialize
{
    /// <summary>
    /// 数据库供应器
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SqlServerConnectionAttribute : DbConfigAttribute
    {
        public override ConnectionConfig GetConfig()
        {
            return new ConnectionConfig
            {
                Name = "de",
                ProviderName = "SqlServer",
                ConnectionString = "Data Source=%DEV-DATABASE-SQLSEVER-DOMAIN%;User ID=%DEV-DATABASE-SQLSEVER-USER%; Password=%DEV-DATABASE-SQLSEVER-PASSWORD%;Initial Catalog=FEI_MALL_FLASH;Pooling=true"//? 数据库链接
            };
        }
    }
}
