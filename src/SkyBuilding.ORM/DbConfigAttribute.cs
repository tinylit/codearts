using System;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 数据库供应器
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public abstract class DbConfigAttribute : Attribute
    {
        /// <summary>
        /// 构造函数（使用默认数据库链接）
        /// </summary>
        public DbConfigAttribute() { }
        /// <summary>
        /// 获取数据库链接配置
        /// </summary>
        /// <returns></returns>
        public abstract ConnectionConfig GetConfig();
    }
}
