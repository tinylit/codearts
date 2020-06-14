using CodeArts;
using CodeArts.ORM;
using CodeArts.ORM.Tests.Serialize;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace UnitTest.Domain.Entities
{
    /// <summary>
    /// 用户实体
    /// </summary>
    [Naming(NamingType.UrlCase, Name = "fei_users")]
    public class FeiUsers : BaseEntity<int>
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Key]
        [Naming("uid")]
        [ReadOnly(true)]
        public override int Id { get; set; }

        /// <summary>
        /// 公司ID
        /// </summary>
        public int Bcid { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// 电话
        /// </summary>
        public string Mobile { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// 角色组
        /// </summary>
        public int Mallagid { get; set; }

        /// <summary>
        /// 盐
        /// </summary>
        public string Salt { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public int? Userstatus { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [DateTimeToken]
        public DateTime ModifiedTime { get; set; }

        /// <summary>
        /// 动作权限列表
        /// </summary>
        //public string Actionlist { get; set; }
    }
}
