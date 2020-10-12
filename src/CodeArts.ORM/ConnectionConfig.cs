using System;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据库连接字符配置
    /// </summary>
    [Serializable]
    public sealed class ConnectionConfig : IReadOnlyConnectionConfig
    {
        /// <summary> 连接名称 </summary>
        public string Name { get; set; }

        /// <summary> 数据库驱动名称 </summary>
        public string ProviderName { get; set; }

        /// <summary> 连接字符串 </summary>
        public string ConnectionString { get; set; }
    }
}
