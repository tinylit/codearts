namespace CodeArts.Proxies
{
    /// <summary>
    /// 具有无参构造函数的代理。
    /// </summary>
    /// <typeparam name="T">代理类型。</typeparam>
    public interface IDefaultConstructorProxyOf<T> where T : class, new()
    {
        /// <summary>
        /// 新建代理对象。
        /// </summary>
        /// <param name="interceptor">拦截器</param>
        /// <returns></returns>
        T New(IInterceptor interceptor);
    }
}
