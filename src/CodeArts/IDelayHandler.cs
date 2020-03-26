using System;

namespace CodeArts
{
    /// <summary>
    /// 延时处理器
    /// </summary>
    /// <typeparam name="TException"></typeparam>
    public interface IDelayHandler<TException> : IHandler<TException> where TException : Exception
    {
        /// <summary>
        /// 延时：<see cref="System.Threading.Thread.Sleep(int)"/>。
        /// </summary>
        /// <param name="e">异常</param>
        /// <param name="times">第N次尝试</param>
        /// <returns>沉睡时长，单位：毫秒</returns>
        int Delay(TException e, int times);
    }
}
