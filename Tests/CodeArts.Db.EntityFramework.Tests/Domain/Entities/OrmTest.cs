using System;

namespace CodeArts.Db.Tests.Domain.Entities
{
    /// <summary>
    /// ORM性能测试。
    /// </summary>
    [Naming("yep_orm_test", NamingType.UrlCase)]
    public class OrmTest : BaseEntity<long>
    {
        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 状态。
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 创建日期。
        /// </summary>
        public DateTime Created { get; set; } = DateTime.Now;

        /// <summary>
        /// 修改日期。
        /// </summary>
        public DateTime Modified { get; set; } = DateTime.Now;
    }
}
