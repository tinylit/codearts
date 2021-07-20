#if NET45_OR_GREATER || NETSTANDARD2_0
using System.Threading;

namespace System.Collections.Generic
{
    /// <summary>
    /// 异步迭代能力。
    /// </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    public interface IAsyncEnumerable<out T>
    {
        /// <summary>
        /// 获取迭代器。
        /// </summary>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken);
    }
}
#endif