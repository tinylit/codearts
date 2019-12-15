using CodeArts;
using CodeArts.ORM;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using UnitTest.Enums;

namespace UnitTest.Domain.Entities
{
    /// <summary>
    /// 机构权限树
    /// </summary>
    [Naming("yep_auth_tree", NamingType.UrlCase)]
    public class AuthTree : BaseEntity<int>
    {

        [Key]
        [ReadOnly(true)]
        public override int Id { get => base.Id; set => base.Id = value; }
        /// <summary>
        /// 父节点
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int DispOrder { get; set; }

        /// <summary>
        /// 包含子节点
        /// </summary>
        public bool HasChild { get; set; }
        /// <summary>
        /// 编号
        /// </summary>
        [Display(Name = "权限编码")]
        [Required(ErrorMessage = "{0}为必填项!")]
        public string Code { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        [Display(Name = "权限名称")]
        [Required(ErrorMessage = "{0}为必填项!")]
        public string Name { get; set; }
        /// <summary>
        /// 权限地址（类型为Page或Project时有效）
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        [Display(Name = "类型")]
        [EnumDataType(typeof(AuthTreeEnum), ErrorMessage = "{0},类型不匹配!")]
        public AuthTreeEnum Type { get; set; }
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
        [Required(ErrorMessage = "{0}为必填项!")]
        public DateTime Modified { get; set; }
    }
}
