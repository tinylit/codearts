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
                ConnectionString = @"Data Source=(local)\SQL2008R2SP2;User ID=sa; Password=Password12!;Initial Catalog=master;Pooling=true"//? 数据库链接
            };
        }
    }
}
