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
        public SelectAttribute(string sql, bool required = false) : base(sql, CommandTypes.Select)
        {
            Required = required;
        }
        /// <summary>
        /// 是否必须。
        /// </summary>
        public bool Required { get; }
    }
}
