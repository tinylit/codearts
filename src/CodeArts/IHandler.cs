using System;

namespace CodeArts
{
    /// <summary>
    /// 尝试
    /// </summary>
    public interface IHandler<TException> where TException : Exception
    {
        /// <summary>
        /// 是否继续。
        /// </summary>
        /// <param name="e">异常</param>
        /// <param name="times">第N次尝试</param>
        /// <returns></returns>
        bool CanDo(TException e, int times);
    }
}
