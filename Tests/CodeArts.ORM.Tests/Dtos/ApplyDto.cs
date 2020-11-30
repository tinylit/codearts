using System;
using System.Collections.Generic;
using System.Text;

namespace CodeArts.ORM.Tests.Dtos
{
    /// <summary>
    /// 发票DTO。
    /// </summary>
    public class ApplyDto
    {
        /// <summary>
        /// 商铺ID。
        /// </summary>
        public ulong ShopId { get; set; }

        /// <summary>
        /// 盘编号(默认：获取公司注册的第一个开票设备，当前公司或商铺有多个开票设备时，请指定开票机号)。
        /// </summary>
        public string MachineCode { get; set; }

        /// <summary>
        /// 开票类型。
        /// </summary>
        public InvoiceTypeEnum InvoiceType { get; set; }

        /// <summary>
        /// 请求平台。
        /// </summary>
        public RequestPlatformEnum RequestPlatform { get; set; } = RequestPlatformEnum.Normal;

        /// <summary>
        /// 发票代码。
        /// </summary>
        public string InvoiceCode { get; set; }

        /// <summary>
        /// 发票号码。
        /// </summary>
        public string InvoiceNo { get; set; }

        /// <summary>
        /// 订单编号。
        /// </summary>
        public string Ddbh { get; set; }

        /// <summary>
        /// 电商订单号。
        /// </summary>
        public string Dsddh { get; set; }

        /// <summary>
        /// 特殊票种。
        /// </summary>
        public TspzTypeEnum Tspz { get; set; } = TspzTypeEnum.Normal;

        /// <summary>
        /// 业务日期。
        /// </summary>
        public DateTime Ywrq { get; set; }

        /// <summary>
        /// 自动开票 => {0:手动,1:自动}。
        /// </summary>
        public int AutoKp { get; set; }

        /// <summary>
        /// 购买方纳税人识别号。
        /// </summary>
        public string Gmfsbh { get; set; }

        /// <summary>
        /// 购买方名称。
        /// </summary>
        public string Gmfmc { get; set; }

        /// <summary>
        /// 购买方地址及电话。
        /// </summary>
        public string Gmfdzdh { get; set; }

        /// <summary>
        /// 购买方开户行及账号。
        /// </summary>
        public string Gmfkhhjzh { get; set; }

        /// <summary>
        /// 收票人手机号。
        /// </summary>
        public string Sprsjh { get; set; }

        /// <summary>
        /// 收票人邮箱。
        /// </summary>
        public string Spryx { get; set; }

        /// <summary>
        /// 价税合计金额。
        /// </summary>
        public decimal Jshj { get; set; }

        /// <summary>
        /// 收款人。
        /// </summary>
        public string Skr { get; set; }

        /// <summary>
        /// 复核人。
        /// </summary>
        public string Fhr { get; set; }

        /// <summary>
        /// 开票人。
        /// </summary>
        public string Kpr { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        public string Bz { get; set; }

        /// <summary>
        /// 明细。
        /// </summary>
        public List<ApplyDetailDto> Mx { get; set; }
    }
}
