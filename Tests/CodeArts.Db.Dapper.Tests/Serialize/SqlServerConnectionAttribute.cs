using CodeArts.Db;
using CodeArts.Db.Tests;
using System;

namespace UnitTest.Serialize
{
    /// <summary>
    /// 数据库供应器。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class SqlServerConnectionAttribute : DbConfigAttribute
    {
        public override ConnectionConfig GetConfig()
        {
            return new ConnectionConfig
            {
                Name = "de",
                ProviderName = "SqlServer",
                ConnectionString = string.Format(@"Data Source={0};User ID={2}; Password={3};Initial Catalog={1};Pooling=true",
                SqlServerConsts.Domain,
                SqlServerConsts.Database,
                SqlServerConsts.User,
                SqlServerConsts.Password)//? 数据库链接
            };
        }
    }
}
