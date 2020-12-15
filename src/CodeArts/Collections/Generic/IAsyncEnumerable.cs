#if NET_NORMAL || NETSTANDARD2_0
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
        /// <returns></returns>
        IAsyncEnumerator<T> GetAsyncEnumerator();
    }
}
#endif