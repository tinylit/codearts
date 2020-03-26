using System;
using System.Net;
using System.Threading.Tasks;

namespace CodeArts
{
    /// <summary>
    /// 尝试
    /// </summary>
    public interface IWebExceptionHanlder<T>
    {
        /// <summary>
        /// 是否继续。
        /// </summary>
        /// <param name="e">异常</param>
        /// <param name="times">第N次尝试</param>
        /// <returns></returns>
        bool CanDo(WebException e, int times);

        /// <summary>
        /// 执行事件
        /// </summary>
        /// <param name="things">事情</param>
        T Do(Func<T> things);

#if !NET40

        /// <summary>
        /// 执行事件
        /// </summary>
        /// <param name="things">事情</param>
        Task<T> Do(Func<Task<T>> things);
#endif

    }
}
