using SkyBuilding;
using SkyBuilding.ORM;
using System;
using System.ComponentModel.DataAnnotations;
using UnitTest.Enums;

namespace UnitTest.Domain.Entities
{
    /// <summary>
    /// 权限关系表
    /// </summary>
    [Naming("yep_auth_ship", NamingType.UrlCase)]
    public class AuthShip : BaseEntity<long>
    {
        /// <summary>
        /// 拥有者ID
        /// </summary>
        [Key]
        public ulong OwnerId { get; set; }
        /// <summary>
        /// 权限ID
        /// </summary>
        public int AuthId { get; set; }
        /// <summary>
        /// 关系类型
        /// </summary>
        [Display(Name = "关系类型")]
        [EnumDataType(typeof(AuthShipEnum), ErrorMessage = "{0},类型不匹配!")]
        public AuthShipEnum Type { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        [Display(Name = "状态")]
        [EnumDataType(typeof(CommonStatusEnum), ErrorMessage = "{0},类型不匹配!")]
        public CommonStatusEnum Status { get; set; }
        /// <summary>
        /// 创建日期时间戳
        /// </summary>
        public DateTime Created { get; set; }
        /// <summary>
        /// 修改日期时间戳
        /// </summary>
        [Display(Name = "最后一次更新时间")]
        [Required(ErrorMessage = "{0}是必填项!")]
        public DateTime Modified { get; set; }
    }
}
