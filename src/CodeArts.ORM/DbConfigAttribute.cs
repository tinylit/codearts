using CodeArts.Config;
using System;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据库连接。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class DbConfigAttribute : Attribute
    {
        private readonly string configName;

        /// <summary>
        /// 默认数据库连接配置。
        /// </summary>
#if NETSTANDARD2_0
        public const string DefaultConfigName = "connectionStrings:default";
#else
        public const string DefaultConfigName = "connectionStrings/default";
#endif

        /// <summary>
        /// 构造函数（使用默认数据库链接【connectionStrings:default】）。
        /// </summary>
        public DbConfigAttribute() : this(DefaultConfigName) { }

        /// <summary>
        /// 构造函数（使用默认数据库链接）。
        /// </summary>
        public DbConfigAttribute(string configName) => this.configName = configName ?? throw new ArgumentNullException(nameof(configName));

        /// <summary>
        /// 获取数据库链接配置。
        /// </summary>
        /// <returns></returns>
        public virtual ConnectionConfig GetConfig() => configName.Config<ConnectionConfig>();
    }
}
