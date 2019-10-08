using System;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 令牌（作为更新数据库的唯一标识，会体现在更新的条件语句中）
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class TokenAttribute : Attribute
    {
        /// <summary>
        /// 创建新令牌
        /// </summary>
        /// <returns></returns>
        public abstract object Create();
    }
}
