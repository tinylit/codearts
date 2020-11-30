using System;

namespace CodeArts.Mvc
{
    /// <summary>
    /// 异常适配器。
    /// </summary>
    public abstract class ExceptionAdapter
    {
        /// <summary>
        /// 能否解决。
        /// </summary>
        /// <param name="error">异常。</param>
        /// <returns></returns>
        public abstract bool CanResolve(Exception error);

        /// <summary>
        /// 获取结果。
        /// </summary>
        /// <param name="error">错误信息。</param>
        /// <returns></returns>
        public abstract DResult GetResult(Exception error);
    }

    /// <summary>
    /// 异常适配器。
    /// </summary>
    /// <typeparam name="T">异常类型。</typeparam>
    public abstract class ExceptionAdapter<T> : ExceptionAdapter where T : Exception
    {
        /// <summary>
        /// 能否解决。
        /// </summary>
        /// <param name="error">异常。</param>
        /// <returns></returns>
        public sealed override bool CanResolve(Exception error) => error is T;

        /// <summary>
        /// 获得结果。
        /// </summary>
        /// <param name="error">异常。</param>
        /// <returns></returns>
        public sealed override DResult GetResult(Exception error)
        {
            try
            {
                return GetResult(error as T);
            }
            catch (Exception e)
            {
                return DResult.Error(e.Message, StatusCodes.BusiError);
            }
        }

        /// <summary>
        /// 获取结果。
        /// </summary>
        /// <param name="error">错误信息。</param>
        /// <returns></returns>
        protected abstract DResult GetResult(T error);
    }
}
