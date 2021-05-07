using System.ComponentModel;

namespace UnitTest.Enums
{
    /// <summary>
    /// OSS 加密算法。
    /// </summary>
    public enum OssKmsEncryptionAlgorithmEnum
    {
        /// <summary>
        /// 无。
        /// </summary>
        [Description("无")]
        None,
        /// <summary>
        /// AES256。
        /// </summary>
        [Description("AES256")]
        AES256,

        /// <summary>
        /// SM4。
        /// </summary>
        [Description("SM4")]
        SM4
    }
}
