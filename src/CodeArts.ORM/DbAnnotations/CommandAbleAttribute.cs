using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 命令执行力（被标记的方法所有参数须指定<see cref="ObjectiveAttribute"/>标记）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAbleAttribute : CommandAttribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="commandType">命令。</param>
        public CommandAbleAttribute(CommandKind commandType) : base(commandType)
        {
        }
    }
}
