using System;

namespace SkyBuilding.AOP
{
    /// <summary>
    /// 拦截
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class InterceptAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="interceptorServiceType">拦截器服务类型</param>
        public InterceptAttribute(Type interceptorServiceType)
        {
            Interceptor = (IInterceptor)Activator.CreateInstance(interceptorServiceType);
        }

        /// <summary>
        /// 拦截器
        /// </summary>
        public IInterceptor Interceptor { get; }
    }
}
