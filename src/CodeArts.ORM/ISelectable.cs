using System.Collections.Generic;

namespace CodeArts.ORM
{
    /// <summary>
    /// 查询能力
    /// </summary>
    public interface ISelectable
    {
        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="required">是否必须</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns></returns>
        TResult QueryFirst<TResult>(SQL sql, object param = null, bool required = true, int? commandTimeout = null);

        /// <summary>
        /// 查询所有结果。
        /// </summary>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns></returns>
        IEnumerable<TResult> Query<TResult>(SQL sql, object param = null, int? commandTimeout = null);
    }
}
