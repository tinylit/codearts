using System;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 数据库供应器
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DbConfigAttribute : Attribute
    {
        private readonly string configName;

        /// <summary>
        /// 构造函数（使用默认数据库链接）
        /// </summary>
        protected DbConfigAttribute() { }

        /// <summary>
        /// 构造函数（使用默认数据库链接）
        /// </summary>
        public DbConfigAttribute(string configName) => this.configName = configName;
        /// <summary>
        /// 获取数据库链接配置
        /// </summary>
        /// <returns></returns>
        public virtual ConnectionConfig GetConfig() => configName.Config<ConnectionConfig>();
    }
}
