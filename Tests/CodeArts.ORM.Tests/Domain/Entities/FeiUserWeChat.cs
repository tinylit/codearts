using CodeArts;
using CodeArts.ORM;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace UnitTest.Domain.Entities
{
    public enum FeiUserWeChatStatusEnum
    {
        /// <summary>
        /// 删除
        /// </summary>
        [Description("已删除")]
        Deleted = -1,
        /// <summary>
        /// 未激活
        /// </summary>
        [Description("未激活")]
        NonActivated = 0,
        /// <summary>
        /// 启用
        /// </summary>
        [Description("启用")]
        Enabled = 1,
        /// <summary>
        /// 禁用
        /// </summary>
        [Description("禁用")]
        Disabled = 2,
        /// <summary>
        /// 异常
        /// </summary>
        [Description("异常")]
        Exception = 3
    }

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
        public FeiUserWeChatStatusEnum Status { get; set; }
    }
}
