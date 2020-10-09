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
        public SelectAttribute(string sql) : base(sql, CommandTypes.Select)
        {
        }
    }
}
