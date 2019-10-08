using SkyBuilding;
using SkyBuilding.ORM;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace UnitTest.Domain.Entities
{
    /// <summary>
    /// 用户微信消息
    /// </summary>
    [Naming("fei_user_wx_account_info", NamingType.UrlCase)]
    public class FeiUserWeChat : BaseEntity<int>
    {
        [Key]
        [ReadOnly(true)]
        public override int Id { get => base.Id; set => base.Id = value; }

        /// <summary>
        /// 用户编号
        /// </summary>
        public int Uid { get; set; }

        /// <summary>
        /// 微信ID
        /// </summary>
        public string Appid { get; set; }

        /// <summary>
        /// 开放ID
        /// </summary>
        public string Openid { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public int Status { get; set; }
    }
}
