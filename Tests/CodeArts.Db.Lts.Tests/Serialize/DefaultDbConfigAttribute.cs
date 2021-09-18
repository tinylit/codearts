using CodeArts.Db;
using CodeArts.Db.Lts.Tests;
using System;

namespace UnitTest.Serialize
{
    /// <summary>
    /// 默认数据库。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DefaultDbConfigAttribute : DbConfigAttribute
    {
        /// <summary>
        /// 获取链接配置。
        /// </summary>
        /// <returns></returns>
        public override ConnectionConfig GetConfig()
        {
            return new ConnectionConfig
            {
                Name = "yep.v3.auth",
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
