using CodeArts;
using CodeArts.ORM;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Mvc.Core.Domain.Entities
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
        /// 用户名称
        /// </summary>
        [Display(Name = "用户名称")]
        [Required(ErrorMessage = "{0}为必填项!")]
        public string Name { get; set; }
    }
}
