using System;
using System.ComponentModel.DataAnnotations;

namespace CodeArts.Db.Dapper.Tests.Domain.Entities
{
    /// <summary>
    /// 用户详情。
    /// </summary>
    [Naming("fei_userdetails", NamingType.UrlCase)]
    public class FeiUserdetails : BaseEntity<int>
    {
        /// <summary>
        /// 用户ID。
        /// </summary>
        [Key]
        [Naming("uid")]
        public override int Id { get; set; }

        /// <summary>
        /// 访问时间。
        /// </summary>
        public DateTime Lastvisittime { get; set; }

        /// <summary>
        /// 访问IP。
        /// </summary>
        public string Lastvisitip { get; set; }

        /// <summary>
        /// 访问区域。
        /// </summary>
        public int Lastvisitrgid { get; set; }

        /// <summary>
        /// 注册时间。
        /// </summary>
        public DateTime Registertime { get; set; }

        /// <summary>
        /// 注册IP。
        /// </summary>
        public string Registerip { get; set; }

        /// <summary>
        /// 注册区域。
        /// </summary>
        public int Registerrgid { get; set; }

        /// <summary>
        /// 性别。
        /// </summary>
        public int Gender { get; set; }

        /// <summary>
        /// 出身日期。
        /// </summary>
        public DateTime Bday { get; set; }

        /// <summary>
        /// 身份证号。
        /// </summary>
        public string Idcard { get; set; }

        /// <summary>
        /// 区域ID。
        /// </summary>
        public int Regionid { get; set; }

        /// <summary>
        /// 地址。
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 简介。
        /// </summary>
        public string Bio { get; set; }

        /// <summary>
        /// 头像。
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// 真实名称。
        /// </summary>
        public string Realname { get; set; }

        /// <summary>
        /// 别名。
        /// </summary>
        public string Nickname { get; set; }
    }
}
