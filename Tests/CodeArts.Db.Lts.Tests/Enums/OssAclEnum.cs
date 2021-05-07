using System.ComponentModel;

namespace UnitTest.Enums
{
    /// <summary>
    /// OSS 读写权限。
    /// </summary>
    public enum OssAclEnum
    {
        /// <summary>
        /// 私有。
        /// </summary>
        [Description("私有")]
        Private,

        /// <summary>
        /// 公共读。
        /// </summary>
        [Description("公共读")]
        PublicRead,

        /// <summary>
        /// 公共读写。
        /// </summary>
        [Description("公共读写")]
        PublicReadWrite
    }
}
