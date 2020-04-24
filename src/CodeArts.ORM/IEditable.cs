using System.Linq.Expressions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 可编辑能力
    /// </summary>
    public interface IEditable
    {
        /// <summary>
        /// SQL矫正
        /// </summary>
        ISQLCorrectSimSettings Settings { get; }

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="sql">SQL语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        int Insert(SQL sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="sql">SQL语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        int Update(SQL sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="sql">SQL语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        int Delete(SQL sql, object param = null, int? commandTimeout = null);
    }

    /// <summary>
    /// 可编辑能力
    /// </summary>
    /// <typeparam name="T">项</typeparam>
    public interface IEditable<T> : IEditable
    {
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        int Excute(Expression expression);
    }
}
