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
                ConnectionString = @"Server=(local)\SQL2016;Database=master;User ID=sa;Password=Password12!"//? 数据库链接
            };
        }
    }
}
