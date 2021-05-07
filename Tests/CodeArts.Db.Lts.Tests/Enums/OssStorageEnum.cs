using System.ComponentModel;

namespace UnitTest.Enums
{
    /// <summary>
    /// OSS 存储类型。
    /// </summary>
    public enum OssStorageEnum
    {
        /// <summary>
        /// 标准。
        /// </summary>
        [Description("标准")]
        Standard,

        /// <summary>
        /// 低频访问。
        /// </summary>
        [Description("低频访问")]
        LowAccess,

        /// <summary>
        /// 归档。
        /// </summary>
        [Description("归档")]
        Archived
    }
}
