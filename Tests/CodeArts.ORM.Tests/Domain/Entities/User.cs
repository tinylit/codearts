using CodeArts;
using CodeArts.ORM;
using CodeArts.ORM.Tests.Serialize;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using UnitTest.Enums;

namespace UnitTest.Domain.Entities
{
    /// <summary>
    /// 用户信息表
    /// </summary>
    [Naming("yep_users", NamingType.UrlCase)]
    public class User : BaseEntity<ulong>
    {
        /// <summary>
        /// 机构ID
        /// </summary>
        public ulong OrgId { get; set; }
        /// <summary>
        /// 公司ID
        /// </summary>
        public long CompanyId { get; set; }
        /// <summary>
        /// 账户
        /// </summary>
        [Display(Name = "用户账户")]
        [Required(ErrorMessage = "{0}为必填项!")]
        public string Account { get; set; }
        /// <summary>
        /// 角色
        /// </summary>
        [Display(Name = "用户角色")]
        [EnumDataType(typeof(UserRole), ErrorMessage = "{0},类型不匹配!")]
        public UserRole Role { get; set; }
        /// <summary>
        /// 用户名称
        /// </summary>
        [Display(Name = "用户名称")]
        [Required(ErrorMessage = "{0}为必填项!")]
        public string Name { get; set; }
        /// <summary>
        /// 微信ID
        /// </summary>
        [Display(Name = "微信ID")]
        [Required(AllowEmptyStrings = true)]
        public string WechatId { get; set; } = string.Empty;
        /// <summary>
        /// 支付宝ID
        /// </summary>
        [Display(Name = "支付宝ID")]
        [Required(AllowEmptyStrings = true)]
        public string AlipayId { get; set; } = string.Empty;
        /// <summary>
        /// 手机号
        /// </summary>
        [Display(Name = "联系电话")]
        [Phone(ErrorMessage = "无效的手机号({0})!")]
        public string Tel { get; set; } = string.Empty;
        /// <summary>
        /// 邮箱
        /// </summary>
        [Display(Name = "邮箱地址")]
        [EmailAddress(ErrorMessage = "无效有效地址({0})!")]
        public string Mail { get; set; } = string.Empty;
        /// <summary>
        /// 头像
        /// </summary>
        public string Avatar { get; set; } = string.Empty;
        /// <summary>
        /// 性别
        /// </summary>
        [Display(Name = "用户性别")]
        [EnumDataType(typeof(UserSex), ErrorMessage = "{0},类型不匹配!")]
        public UserSex Sex { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        [Required]
        [Display(Name = "密码")]
        [RegularExpression(@"^(?![0-9]+$)(?![a-zA-Z]+$)(?![_!@#$%^&*]+$)[0-9a-zA-Z_!.@#$%^&*]+$", ErrorMessage = "{0}必须为数字、字母、特殊符号(_!.@#$%^&*)，且必须至少包含任意两种。")]
        public string Password { get; set; }
        /// <summary>
        /// 加盐
        /// </summary>
        public string Salt { get; set; } = string.Empty;
        /// <summary>
        /// 状态
        /// </summary>
        [Display(Name = "用户状态")]
        [EnumDataType(typeof(CommonStatusEnum), ErrorMessage = "{0},类型不匹配!")]
        public CommonStatusEnum Status { get; set; }
        /// <summary>
        /// 扩展信息
        /// </summary>
        public int ExtendsEnum { get; set; }
        /// <summary>
        /// 创建日期时间戳
        /// </summary>
        public DateTime Registered { get; set; }
        /// <summary>
        /// 修改日期时间戳
        /// </summary>
        [DateTimeToken]
        [Display(Name = "最后一次更新时间")]
        [Required(ErrorMessage = "{0}为必填项!")]
        public DateTime Modified { get; set; }
    }
}
