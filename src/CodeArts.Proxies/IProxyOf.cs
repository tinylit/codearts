namespace CodeArts.Proxies
{
    /// <summary>
    /// 代理
    /// </summary>
    /// <typeparam name="T">代理类型</typeparam>
    public interface IProxyOf<T> where T : class
    {
        /// <summary>
        /// 代理对象。
        /// </summary>
        /// <param name="instance">类型实例</param>
        /// <param name="interceptor">拦截器</param>
        /// <returns></returns>
        T Of(T instance, IInterceptor interceptor);
    }
}
