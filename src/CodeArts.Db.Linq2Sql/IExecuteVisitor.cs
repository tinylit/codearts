using System.Data;

namespace CodeArts.Db
{
    /// <summary>
    /// 执行能力访问器。
    /// </summary>
    public interface IExecuteVisitor : IStartupVisitor
    {
        /// <summary>
        /// SQL语句。
        /// </summary>
        /// <returns></returns>
        CommandSql ToSQL();
    }
}
