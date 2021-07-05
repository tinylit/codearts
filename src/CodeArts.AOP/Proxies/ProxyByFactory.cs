using Microsoft.Extensions.DependencyInjection;
using System;

namespace CodeArts.Proxies
{
    class ProxyByFactory : IProxyByPattern
    {
        private readonly Type serviceType;
        private readonly Func<IServiceProvider, object> implementationFactory;
        private readonly ServiceLifetime lifetime;

        public ProxyByFactory(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime)
        {
            this.serviceType = serviceType;
            this.implementationFactory = implementationFactory;
            this.lifetime = lifetime;
        }

        public ServiceDescriptor Resolve()
        {
            throw new NotImplementedException();
        }
    }
}
