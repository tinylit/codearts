﻿#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{

    /// <summary>
    /// 异步查询器。
    /// </summary>
    public interface IAsyncQueryProvider : IQueryProvider
    {
        /// <summary>
        /// 执行结果。
        /// </summary>
        /// <typeparam name="TResult">结果。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default);
    }
}
#endif