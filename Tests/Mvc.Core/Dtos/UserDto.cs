using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Mvc.Core.Dtos
{
    /// <summary>
    /// 用户实体
    /// </summary>
    public class UserDto
    {

        /// <summary>
        /// 用户ID
        /// </summary>
        [Required]
        public long Id { get; set; }
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
        public string Tel { get; set; }
        /// <summary>
        /// 邮箱
        /// </summary>
        public string Mail { get; set; }
        /// <summary>
        /// 加盐
        /// </summary>
        public string Salt { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 最后一次修改日期时间戳
        /// </summary>
        public DateTime Modified { get; set; }

        /// <summary>
        /// 账户
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// 机构ID
        /// </summary>
        public long OrgId { get; set; }
        /// <summary>
        /// 公司ID
        /// </summary>
        public long CompanyId { get; set; }

        /// <summary>
        /// 创建日期时间戳
        /// </summary>
        public DateTime Registered { get; set; }
    }
}
