namespace CodeArts
{
    /// <summary> 命名规范 </summary>
    public enum NamingType
    {
        /// <summary> 默认命名(由业务决定) </summary>
        None = 0,

        /// <summary> 默认命名(原样) </summary>
        Normal = 1,

        /// <summary> 驼峰命名,如：userName </summary>
        CamelCase = 2,

        /// <summary> url命名,如：user_name，注：反序列化时也需要指明 </summary>
        UrlCase = 3,

        /// <summary> 帕斯卡命名,如：UserName </summary>
        PascalCase = 4
    }
}
