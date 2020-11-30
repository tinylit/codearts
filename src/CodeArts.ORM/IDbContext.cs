using CodeArts.ORM.Common;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据上下文。
    /// </summary>
    public interface IDbContext
    {
        /// <summary>
        /// SQL 矫正。
        /// </summary>
        ISQLCorrectSimSettings Settings { get; }

        /// <summary>
        /// 创建数据库链接。
        /// </summary>
        /// <returns></returns>
        DbConnection CreateDb();
    }

    /// <summary>
    /// 数据上下文。
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IDbContext<TEntity> : IDbContext, IContext where TEntity : class, IEntiy
    {
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        int Execute(Expression expression);

#if NET_NORMAL
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default);
#endif

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns>执行影响行。</returns>
        int Execute(SQL sql, object param = null, int? commandTimeout = null);

#if NET_NORMAL
        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>执行影响行。</returns>
        Task<int> ExecuteAsync(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
#endif

    }
}
