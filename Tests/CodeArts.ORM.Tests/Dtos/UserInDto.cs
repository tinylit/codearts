using System.ComponentModel.DataAnnotations;
using UnitTest.Enums;

namespace UnitTest.Dtos
{
    /// <summary>
    /// 新增用户入参
    /// </summary>
    public class UserInDto
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
        [Required]
        [Display(Name = "用户账户")]
        public string Account { get; set; }
        /// <summary>
        /// 角色
        /// </summary>
        [Display(Name = "用户角色")]
        [EnumDataType(typeof(UserRole))]
        public UserRole Role { get; set; }
        /// <summary>
        /// 用户名称
        /// </summary>
        [Required]
        [Display(Name = "用户名称")]
        public string Name { get; set; }
        /// <summary>
        /// 微信ID
        /// </summary>
        public string WechatId { get; set; }
        /// <summary>
        /// 支付宝ID
        /// </summary>
        public string AlipayId { get; set; }
        /// <summary>
        /// 手机号
        /// </summary>
        [Phone]
        [Display(Name = "联系电话")]
        public string Tel { get; set; }
        /// <summary>
        /// 邮箱
        /// </summary>
        [EmailAddress]
        [Display(Name = "邮箱地址")]
        public string Mail { get; set; }
        /// <summary>
        /// 头像
        /// </summary>
        public string Avatar { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        [Display(Name = "用户性别")]
        [EnumDataType(typeof(UserSex))]
        public UserSex Sex { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        [Required]
        [Display(Name = "密码")]
        [MinLength(8), MaxLength(20)]
        [RegularExpression(@"^(?![0-9]+$)(?![a-zA-Z]+$)(?![_!@#$%^&*]+$)[0-9a-zA-Z_!@#$%^&*]+$", ErrorMessage = "{0}必须为数字、字母、特殊符号(_!@#$%^&*)，且必须至少包含任意两种。")]
        public string Password { get; set; }
        /// <summary>
        /// 加盐
        /// </summary>
        public string Salt { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        [Display(Name = "用户状态")]
        [EnumDataType(typeof(CommonStatusEnum))]
        public CommonStatusEnum Status { get; set; }
        /// <summary>
        /// 扩展信息
        /// </summary>
        public int ExtendsEnum { get; set; }
    }
}
