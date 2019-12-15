namespace CodeArts.Proxies
{
    /// <summary>
    /// 构造函数。
    /// </summary>
    public interface IConstructorProxyOf<T> where T : class
    {
        /// <summary>
        /// 创建代理实例。
        /// </summary>
        /// <param name="interceptor">拦截器</param>
        /// <param name="arguments">构造代理类的函数参数</param>
        /// <returns></returns>
        T CreateInstance(IInterceptor interceptor, params object[] arguments);
    }
}
