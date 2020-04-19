using CodeArts.ORM;
using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 查询语句。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class QueryAttribute : SqlAttribute
    {
        /// <summary>
        /// 查询语句。
        /// </summary>
        public QueryAttribute(SQL sql, bool required = false) : base(sql, CommandKind.Query)
        {
            Required = required;
        }
        /// <summary>
        /// 是否必须。
        /// </summary>
        public bool Required { get; }
    }
}
