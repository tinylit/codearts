namespace CodeArts.Db
{
    /// <summary>
    /// 只读配置。
    /// </summary>
    public interface IReadOnlyConnectionConfig
    {
        /// <summary> 连接名称。 </summary>
        string Name { get; }

        /// <summary> 数据库驱动名称。 </summary>
        string ProviderName { get; }

        /// <summary> 连接字符串。 </summary>
        string ConnectionString { get; }
    }
}
