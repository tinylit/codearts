using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 操作字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class CommandAttribute : Attribute
    {
        /// <summary>
        /// 所有字段。
        /// </summary>
        /// <param name="commandType">命令</param>
        public CommandAttribute(CommandKind commandType)
        {
            CommandType = commandType;
        }
        /// <summary>
        /// 命令。
        /// </summary>
        public CommandKind CommandType { get; }
    }
}
