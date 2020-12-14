using CodeArts;
using CodeArts.Db;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnitTest.Domain.Entities
{
    /// <summary>
    /// 用户实体。
    /// </summary>
    public class FeiUsers
    {
        /// <summary>
        /// 用户ID。
        /// </summary>
        public int UId { get; set; }

        /// <summary>
        /// 公司ID。
        /// </summary>
        public int Bcid { get; set; }

        /// <summary>
        /// 用户名。
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 邮箱。
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 电话。
        /// </summary>
        public string Mobile { get; set; }

        /// <summary>
        /// 密码。
        /// </summary>
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// 角色组。
        /// </summary>
        public short Mallagid { get; set; }

        /// <summary>
        /// 盐。
        /// </summary>
        public string Salt { get; set; }

        /// <summary>
        /// 状态。
        /// </summary>
        public int? Userstatus { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        public DateTime Created_Time { get; set; }

        /// <summary>
        /// 修改时间。
        /// </summary>
        public DateTime Modified_Time { get; set; }

        /// <summary>
        /// 动作权限列表。
        /// </summary>
        //public string Actionlist { get; set; }
    }
}
