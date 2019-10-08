using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest.Dtos
{
    /// <summary>
    /// 税务编码树结构出参
    /// </summary>
    public class TaxCodeTreeSimDto
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 简称
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// 是否包含孩子节点
        /// </summary>
        public bool HasChild { get; set; }
    }
}
