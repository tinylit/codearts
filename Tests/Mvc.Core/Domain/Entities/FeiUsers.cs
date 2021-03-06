﻿using CodeArts;
using CodeArts.Db;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mvc.Core.Domain.Entities
{
    /// <summary>
    /// 用户实体。
    /// </summary>
    [Table("fei_users")]
    public class FeiUsers : BaseEntity<int>
    {
        /// <summary>
        /// 用户ID。
        /// </summary>
        [Key]
        [ReadOnly(true)]
        [Column("uid")]
        public override int Id { get; set; }

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
        [Required]
        [EmailAddress]
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
        [Column("created_time")]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 修改时间。
        /// </summary>
        [Column("modified_time")]
        public DateTime ModifiedTime { get; set; }
    }
}
