using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 删除能力。
    /// </summary>
    public class DeleteableAttribute : CommandAbleAttribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public DeleteableAttribute() : base(CommandKind.Delete)
        {
        }
    }
}
