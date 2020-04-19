using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 查询能力。
    /// </summary>
    public class QueryableAttribute : CommandAbleAttribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public QueryableAttribute() : base(CommandKind.Query)
        {
        }
    }
}
