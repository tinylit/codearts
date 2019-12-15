using CodeArts;
using CodeArts.ORM;
using System;
using System.ComponentModel.DataAnnotations;
using UnitTest.Enums;

namespace UnitTest.Domain.Entities
{
    /// <summary>
    /// 机构树
    /// </summary>
    [Naming("yep_org_tree", NamingType.UrlCase)]
    public class OrgTree : BaseEntity<ulong>
    {
        /// <summary>
        /// 父节点
        /// </summary>
        public ulong ParentId { get; set; }

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int DispOrder { get; set; }

        /// <summary>
        /// 包含子节点
        /// </summary>
        public bool HasChild { get; set; }
        /// <summary>
        /// 机构编码
        /// </summary>
        [Display(Name = "机构编码")]
        [Required(ErrorMessage = "{0}为必填项!")]
        public string Code { get; set; }
        /// <summary>
        /// 机构名称
        /// </summary>
        [Display(Name = "机构名称")]
        [Required(ErrorMessage = "{0}为必填项!")]
        public string Name { get; set; }

        /// <summary>
        /// 机构类型
        /// </summary>
        [Display(Name = "机构类型")]
        [EnumDataType(typeof(OrgTreeEnum))]
        public OrgTreeEnum Type { get; set; }
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
