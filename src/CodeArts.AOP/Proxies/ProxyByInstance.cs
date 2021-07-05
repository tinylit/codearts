using Microsoft.Extensions.DependencyInjection;
using System;

namespace CodeArts.Proxies
{
    class ProxyByInstance : IProxyByPattern
    {
        private readonly Type serviceType;
        private readonly object instance;
        public ProxyByInstance(Type serviceType, object instance)
        {
            this.serviceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public ServiceDescriptor Resolve()
        {
            throw new NotImplementedException();
        }
    }
}
