namespace CodeArts.Proxies
{
    /// <summary>
    /// 拦截器
    /// </summary>
    public interface IInterceptor
    {
        /// <summary>
        /// 拦截
        /// </summary>
        void Intercept(IIntercept intercept);
    }
}
