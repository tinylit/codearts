using CodeArts.ORM;
using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 查询语句。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SelectAttribute : SqlAttribute
    {
        /// <summary>
        /// 查询语句。
        /// </summary>
        public SelectAttribute(string sql, bool required = false, string missingMsg = null) : base(sql, CommandTypes.Select)
        {
            Required = required;
            MissingMsg = missingMsg;
        }
        /// <summary>
        /// 是否必须。
        /// </summary>
        public bool Required { get; }

        /// <summary>
        /// 未查询到数据时的错误消息。
        /// </summary>
        public string MissingMsg { get; }
    }
}
