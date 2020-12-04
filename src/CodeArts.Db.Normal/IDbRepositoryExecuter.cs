using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db
{
    /// <summary>
    /// 仓储执行器。
    /// </summary>
    public interface IDbRepositoryExecuter
    {
        /// <summary>
        /// 创建执行器。
        /// </summary>
        /// <returns></returns>
        IExecuteVisitor CreateExe();

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <returns>执行影响行。</returns>
        int Execute(IDbContext context,  CommandSql commandSql);

#if NET_NORMAL
        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>执行影响行。</returns>
        Task<int> ExecuteAsync(IDbContext context, CommandSql commandSql, CancellationToken cancellationToken = default);
#endif
    }
}
