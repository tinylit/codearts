using CodeArts.Emit;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CodeArts.Proxies
{
    class ProxyByInstance : ProxyByServiceType
    {
        private readonly object instance;

        public ProxyByInstance(ModuleEmitter moduleEmitter, Type serviceType, object instance) : base(moduleEmitter, serviceType, ServiceLifetime.Singleton)
        {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        protected override ServiceDescriptor Resolve(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(serviceType, Activator.CreateInstance(implementationType, instance));
        }
    }
}
