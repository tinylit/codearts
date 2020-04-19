using CodeArts.ORM;
using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 更新语句。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UpdateAttribute : SqlAttribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="sql">SQL</param>
        public UpdateAttribute(SQL sql) : base(sql, CommandKind.Update)
        {
        }
    }
}
