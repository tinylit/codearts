using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 插入能力。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class InsertableAttribute : CommandAbleAttribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public InsertableAttribute() : base(CommandKind.Insert)
        {
        }
    }
}
