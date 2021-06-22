using System;

namespace CodeArts.Db
{
    /// <summary>
    /// 可读写的数据库。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    public class DbWriteConfigAttribute : DbConfigAttribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DbWriteConfigAttribute() : base()
        {
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="configName"></param>
        public DbWriteConfigAttribute(string configName) : base(configName)
        {
        }
    }
}
