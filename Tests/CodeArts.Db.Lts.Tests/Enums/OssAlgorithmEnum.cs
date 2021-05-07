using System.ComponentModel;

namespace UnitTest.Enums
{
    /// <summary>
    /// OSS 加密方式。
    /// </summary>
    public enum OssAlgorithmEnum
    {
        /// <summary>
        /// 无。
        /// </summary>
        [Description("无")]
        None,

        /// <summary>
        /// OSS 完全托管。
        /// </summary>
        [Description("OSS完全托管")]
        OssProxy,

        /// <summary>
        /// KMS。
        /// </summary>
        [Description("KMS")]
        KMS
    }
}
