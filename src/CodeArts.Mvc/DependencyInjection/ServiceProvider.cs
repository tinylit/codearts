#if NET40 || NET_NORMAL
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
#if NET_NORMAL
using System.Web;
#endif

namespace CodeArts.Mvc.DependencyInjection
{
    /// <summary>
    /// The default IServiceProvider.
    /// </summary>
    public sealed class ServiceProvider : IServiceProvider
    {
        private readonly IEnumerable<ServiceDescriptor> serviceDescriptors;

        private readonly ConcurrentDictionary<ServiceDescriptor, object> singletions = new ConcurrentDictionary<ServiceDescriptor, object>();
        private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> implementations = new ConcurrentDictionary<Type, Func<IServiceProvider, object>>();
        private readonly ConcurrentDictionary<Type, ServiceDescriptor> descriptors = new ConcurrentDictionary<Type, ServiceDescriptor>();
#if NET_NORMAL
        private readonly ConcurrentDictionary<HttpContext, ConcurrentDictionary<ServiceDescriptor, object>> scopes = new ConcurrentDictionary<HttpContext, ConcurrentDictionary<ServiceDescriptor, object>>();
#endif
        internal ServiceProvider(IEnumerable<ServiceDescriptor> serviceDescriptors)
        {
            this.serviceDescriptors = serviceDescriptors;
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object GetService(Type serviceType)
        {
            ServiceDescriptor service = serviceDescriptors.FirstOrDefault(x => x.ServiceType == serviceType);

            if (service is null)
            {
                if (serviceType.IsGenericType)
                {
                    if (descriptors.TryGetValue(serviceType, out ServiceDescriptor descriptor))
                    {
                        return GetService(descriptor);
                    }

                    var genericType = serviceType.GetGenericTypeDefinition();

                    service = serviceDescriptors.FirstOrDefault(x => x.ServiceType == serviceType);

                    if (service is null || service.ImplementationType is null)
                    {
                        if (serviceType.IsInterface || serviceType.IsAbstract)
                        {
                            return null;
                        }

                        return GetServiceByImplementationType(serviceType);
                    }

                    descriptors.TryAdd(serviceType, descriptor = new ServiceDescriptor(serviceType, service.ImplementationType.MakeGenericType(serviceType.GetGenericArguments()), service.Lifetime));

                    return GetService(descriptor);
                }

                if (serviceType.IsValueType || serviceType.IsInterface || serviceType.IsAbstract)
                    return null;

                return GetServiceByImplementationType(serviceType);
            }

            return GetService(service);
        }

        private object GetServiceByImplementationType(Type implementationType)
        {
            var constructor = implementationType
                .GetConstructors()
                .Where(x => x.IsPublic)
                .OrderBy(x => x.GetParameters().Length)
                .FirstOrDefault();

            if (constructor is null)
            {
                return null;
            }

            var parameters = constructor.GetParameters();

            if (parameters.Length == 0)
            {
                return constructor.Invoke(new object[0]);
            }

            if (parameters.All(x => serviceDescriptors.Any(y => x.ParameterType == y.ServiceType)))
            {
                return constructor.Invoke(parameters.Select(x => GetService(x.ParameterType)).ToArray());
            }

            return null;
        }

        private object GetService(ServiceDescriptor service)
        {
            switch (service.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    return service.ImplementationInstance ?? singletions.GetOrAdd(service, _ =>
                    {
                        if (service.ImplementationFactory is null)
                        {
                            return implementations
                            .GetOrAdd(service.ImplementationType, MakeImplementationFactory)
                            .Invoke(this);
                        }

                        return service.ImplementationFactory.Invoke(this);
                    });
#if NET_NORMAL
                case ServiceLifetime.Scoped:
                    return service.ImplementationInstance ?? scopes.GetOrAdd(HttpContext.Current, context =>
                    {
                        context.AddOnRequestCompleted(context2 =>
                        {
                            if (scopes.TryRemove(context2, out ConcurrentDictionary<ServiceDescriptor, object> cache))
                            {
                                cache.Clear();
                            }
                        });
                        return new ConcurrentDictionary<ServiceDescriptor, object>();
                    }).GetOrAdd(service, service2 =>
                    {
                        return service2.ImplementationFactory?.Invoke(this) ?? implementations.GetOrAdd(service2.ImplementationType, MakeImplementationFactory).Invoke(this);
                    });
#endif
                case ServiceLifetime.Transient:
                    return service.ImplementationInstance ?? service.ImplementationFactory?.Invoke(this) ?? implementations.GetOrAdd(service.ImplementationType, MakeImplementationFactory).Invoke(this);
                default:
                    return null;
            }
        }

        private Func<IServiceProvider, object> MakeImplementationFactory(Type implementationType)
        {
            var constructor = implementationType
                .GetConstructors()
                .OrderBy(x => x.GetParameters().Length)
                .FirstOrDefault(x => x.IsPublic);

            if (constructor is null)
                return null;

            var parameters = constructor.GetParameters();

            if (parameters.Length == 0)
            {
                return provider => constructor.Invoke(new object[0]);
            }

            if (parameters.All(x => serviceDescriptors.Any(y => x.ParameterType == y.ServiceType)))
                return provider => constructor.Invoke(parameters.Select(x => provider.GetService(x.ParameterType)).ToArray());

            return null;
        }
    }
}
#endif