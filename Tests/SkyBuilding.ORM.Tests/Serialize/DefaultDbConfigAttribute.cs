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
                ConnectionString = "server=127.0.0.1;port=3306;user=root;password=Password12!;database=mysql;"//? 数据库链接
            };
        }
    }
}
