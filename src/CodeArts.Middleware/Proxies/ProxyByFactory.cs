using CodeArts.Emit;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CodeArts.Proxies
{
    class ProxyByFactory : ProxyByServiceType
    {
        private readonly Func<IServiceProvider, object> implementationFactory;

        public ProxyByFactory(ModuleEmitter moduleEmitter, Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime) : base(moduleEmitter, serviceType, lifetime)
        {
            this.implementationFactory = implementationFactory ?? throw new ArgumentNullException(nameof(implementationFactory));
        }
        protected override ServiceDescriptor Resolve(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(serviceType, serviceProvider =>
            {
                var instance = implementationFactory.Invoke(serviceProvider);

                return Activator.CreateInstance(implementationType, instance);

            }, lifetime);
        }
    }
}
