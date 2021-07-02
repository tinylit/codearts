using System;

namespace CodeArts.Proxies
{
    /// <summary>
    /// 代理接口。
    /// </summary>
    public class ProxyByInstance
    {
        private readonly Type serviceType;
        private readonly object instance;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="instance"></param>
        public ProxyByInstance(Type serviceType, object instance)
        {
            this.serviceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }
    }
}
