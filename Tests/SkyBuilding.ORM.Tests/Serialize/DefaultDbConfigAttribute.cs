using SkyBuilding.ORM;
using System;

namespace UnitTest.Serialize
{
    /// <summary>
    /// 默认数据库
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DefaultDbConfigAttribute : DbConfigAttribute
    {
        /// <summary>
        /// 获取链接配置
        /// </summary>
        /// <param name="repository"></param>
        /// <returns></returns>
        public override ConnectionConfig GetConfig()
        {
            return new ConnectionConfig
            {
                Name = "yep.v3.auth",
                ProviderName = "MySql",
                ConnectionString = "server=%DEV-DATABASE-MYSQL-DOMAIN%;port=%DEV-DATABASE-MYSQL-USER%;user=%DEV-DATABASE-MYSQL-PASSWORD%;password=HUHihihhe78393h372h0; database=yep.v3.admin;"//? 数据库链接
            };
        }
    }
}
