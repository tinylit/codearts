using System.Linq.Expressions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 可写仓库。
    /// </summary>
    public interface IWriteRepository
    {
        /// <summary>
        /// SQL矫正。
        /// </summary>
        ISQLCorrectSimSettings Settings { get; }

        /// <summary>
        /// 执行。
        /// </summary>
        /// <param name="sql">SQL语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        int Insert(SQL sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 执行。
        /// </summary>
        /// <param name="sql">SQL语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        int Update(SQL sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 执行。
        /// </summary>
        /// <param name="sql">SQL语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        int Delete(SQL sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 执行。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        int Excute(Expression expression);
    }

    /// <summary>
    /// 可写仓储基本接口。
    /// </summary>
    /// <typeparam name="T">类型。</typeparam>
    public interface IWriteRepository<T> : IWriteRepository where T : class, IEntiy
    {
    }
}
