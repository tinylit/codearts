using CodeArts.ORM;
using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 插入语句
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class InsertAttribute : SqlAttribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="sql">SQL</param>
        public InsertAttribute(string sql) : base(sql, CommandTypes.Insert)
        {
        }
    }
}
