using System.Data;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 执行能力访问器。
    /// </summary>
    public interface IExecuteVisitor : IStartupVisitor
    {
        /// <summary>
        /// 指令行为。
        /// </summary>
        ActionBehavior Behavior { get; }

        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。<see cref="System.Data.IDbCommand.CommandTimeout"/>
        /// </summary>
        int? TimeOut { get; }
    }
}
