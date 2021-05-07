using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using UnitTest.Enums;
using UnitTest.Serialize;

namespace CodeArts.Db.Lts.Tests.Domain.Entities
{
    /// <summary>
    /// OSS。
    /// </summary>
    [SqlServerConnection]
    [Naming(NamingType.UrlCase)]
    public class OssBuckets : BaseEntity<long>
    {
        /// <summary>
        /// 密钥。
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "密钥")]
        public string AppKey { get; set; }

        /// <summary>
        /// 密钥私钥。
        /// </summary>
        [Required]
        [MaxLength(60)]
        [Display(Name = "私钥")]
        public string AppSecret { get; set; }

        /// <summary>
        /// 名称。
        /// </summary>
        [Required]
        [MaxLength(65)]
        [Display(Name = "名称")]
        public string Name { get; set; }

        /// <summary>
        /// 区域。
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "区域")]
        public string Region { get; set; }

        /// <summary>
        /// 存储类型。
        /// </summary>
        [Display(Name = "存储类型")]
        [EnumDataType(typeof(OssStorageEnum))]
        public OssStorageEnum Storage { get; set; }

        /// <summary>
        /// 读写权限。
        /// </summary>
        [Display(Name = "读写权限")]
        [EnumDataType(typeof(OssAclEnum))]
        public OssAclEnum Acl { get; set; }

        /// <summary>
        /// 是否多版本控制。
        /// </summary>
        public bool Multiversions { get; set; }

        /// <summary>
        /// 是否实时查询。
        /// </summary>
        public bool OpenSls { get; set; }

        /// <summary>
        /// 是否定时备份。
        /// </summary>
        public bool OpenHbr { get; set; }

        /// <summary>
        /// 加密方式。
        /// </summary>
        [Display(Name = "加密方式")]
        [EnumDataType(typeof(OssAlgorithmEnum))]
        public OssAlgorithmEnum Algorithm { get; set; }

        /// <summary>
        /// 加密算法。
        /// </summary>
        [Display(Name = "加密算法")]
        [EnumDataType(typeof(OssKmsEncryptionAlgorithmEnum))]
        public OssKmsEncryptionAlgorithmEnum KmsEncryptionAlgorithm { get; set; }

        /// <summary>
        /// 域名。
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Display(Name = "域名")]
        public string Domain { get; set; }

        /// <summary>
        /// 是否启用。
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 创建日期。
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
