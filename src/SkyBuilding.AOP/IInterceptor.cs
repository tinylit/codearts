namespace SkyBuilding.AOP
{
    /// <summary>
    /// 拦截器
    /// </summary>
    public interface IInterceptor
    {
        /// <summary>
        /// 拦截
        /// </summary>
        void Intercept(IInvokeBinder invokeBinder);
    }
}
