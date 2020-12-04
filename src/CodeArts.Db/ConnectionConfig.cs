using System;

namespace CodeArts.Db
{
    /// <summary>
    /// 数据库连接字符配置。
    /// </summary>
    [Serializable]
    public sealed class ConnectionConfig : IConfigable<ConnectionConfig>, IReadOnlyConnectionConfig
    {
        /// <summary> 连接名称。 </summary>
        public string Name { get; set; }

        /// <summary> 数据库驱动名称。 </summary>
        public string ProviderName { get; set; }

        /// <summary> 连接字符串。 </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 监听到变更后的新数据。
        /// </summary>
        /// <param name="changedValue">变更后的数据。</param>
        public void SaveChanges(ConnectionConfig changedValue)
        {
            if (changedValue is null)
            {
                return;
            }

            Name = changedValue.Name;
            ProviderName = changedValue.ProviderName;
            ConnectionString = changedValue.ConnectionString;
        }
    }
}
