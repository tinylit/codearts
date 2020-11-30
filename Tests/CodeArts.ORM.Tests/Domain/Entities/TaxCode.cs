using CodeArts;
using CodeArts.ORM;
using System;
using System.ComponentModel.DataAnnotations;

namespace UnitTest.Domain.Entities
{
    /// <summary>
    /// 商品税务编码。
    /// </summary>
    [Naming("yep_tax_code", NamingType.UrlCase)]
    public class TaxCode : BaseEntity<string>
    {
        /// <summary>
        /// 父级ID。
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// 层级。
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 名称。
        /// </summary>
        [Display(Name = "名称")]
        [Required, MaxLength(256 / 2)]
        public string Name { get; set; }

        /// <summary>
        /// 简称。
        /// </summary>
        [Display(Name = "简称")]
        [Required(AllowEmptyStrings = true), MaxLength(128 / 2)]
        public string ShortName { get; set; }

        /// <summary>
        /// 规格型号。
        /// </summary>
        [Display(Name = "规格型号")]
        [Required(AllowEmptyStrings = true), MaxLength(16 / 2)]
        public string Specification { get; set; }

        /// <summary>
        /// 单位。
        /// </summary>
        [Display(Name = "单位")]
        [Required(AllowEmptyStrings = true), MaxLength(16 / 2)]
        public string Unit { get; set; }

        /// <summary>
        /// 单价。
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 是否使用优惠政策。
        /// </summary>
        public bool UsePolicy { get; set; }

        /// <summary>
        /// 优惠政策。
        /// </summary>
        public byte PolicyType { get; set; }

        /// <summary>
        /// 税率。
        /// </summary>
        public double TaxRate { get; set; }

        /// <summary>
        /// 免税率类型。
        /// </summary>
        public byte FreeTaxType { get; set; }

        /// <summary>
        /// 含税标志。
        /// </summary>
        public bool HasTax { get; set; }

        /// <summary>
        /// 增值税特殊管理。
        /// </summary>
        [Display(Name = "增值税特殊管理")]
        [Required(AllowEmptyStrings = true), MaxLength(128 / 2)]
        public string SpecialManage { get; set; }

        /// <summary>
        /// 介绍。
        /// </summary>
        [Display(Name = "介绍")]
        //[Required(AllowEmptyStrings = true), MaxLength(2048 / 2)]
        public string Introduction { get; set; }

        /// <summary>
        /// 状态。
        /// </summary>
        [Display(Name = "状态")]
        public int Status { get; set; }

        /// <summary>
        /// 创建日期。
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改日期。
        /// </summary>
        [Display(Name = "修改日期")]
        public DateTime ModifyTime { get; set; }
    }
}
