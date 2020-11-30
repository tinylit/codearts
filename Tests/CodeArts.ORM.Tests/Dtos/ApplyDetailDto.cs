using System;
using System.Collections.Generic;
using System.Text;

namespace CodeArts.ORM.Tests.Dtos
{
    /// <summary>
    /// 业务明细DTO。
    /// </summary>
    public class ApplyDetailDto
    {
        /// <summary>
        /// 商品名称。
        /// </summary>
        public string Spmc { get; set; }

        /// <summary>
        /// 规格型号。
        /// </summary>
        public string Ggxh { get; set; }

        /// <summary>
        /// 电商订单号。
        /// </summary>
        public string Dsddh { get; set; }

        /// <summary>
        /// 计量单位。
        /// </summary>
        public string Dw { get; set; }

        /// <summary>
        /// 商品数量 小数点后 6 位。
        /// </summary>
        public decimal Spsl { get; set; }

        /// <summary>
        /// 商品单价 小数点后 6 位含税。
        /// </summary>
        public decimal Dj { get; set; }

        /// <summary>
        /// 金额 含税，单位：元（2 位小数）。
        /// </summary>
        public decimal Je { get; set; }

        /// <summary>
        /// 税率 2 位小数，例 1%为0.01；17%为0.17 。
        /// </summary>
        public decimal Sl { get; set; }

        /// <summary>
        /// 商品税务编码。
        /// </summary>
        public string Spbm { get; set; }

        /// <summary>
        /// 发票行性质。
        /// </summary>
        public int Fphxz { get; set; }

        /// <summary>
        /// 零税率标识。
        /// </summary>
        public string Lslbs { get; set; }

        /// <summary>
        /// 优惠政策标识。
        /// </summary>
        public string Yhzcbs { get; set; }
    }
}
