﻿#if NET45_OR_GREATER || NETSTANDARD2_0
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    /// <summary>
    /// 异步迭代器。
    /// </summary>
    /// <typeparam name="T">结果类型。</typeparam>
    public interface IAsyncEnumerator<out T> : IDisposable
    {
        /// <summary>
        /// 当前数据。
        /// </summary>
        T Current { get; }

        /// <summary>
        /// 下一份数据。
        /// </summary>
        /// <returns></returns>
        Task<bool> MoveNextAsync();
    }
}
#endif