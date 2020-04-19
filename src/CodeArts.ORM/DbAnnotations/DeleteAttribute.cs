using CodeArts.ORM;
using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 删除语句
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DeleteAttribute : SqlAttribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="sql">SQL</param>
        public DeleteAttribute(SQL sql) : base(sql, CommandKind.Delete)
        {
        }
    }
}
