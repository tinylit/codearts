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

        private readonly ConcurrentDictionary<Type, ServiceDescriptor> descriptors = new ConcurrentDictionary<Type, ServiceDescriptor>();
#if NET_NORMAL
        private readonly ConcurrentDictionary<HttpContext, ConcurrentDictionary<ServiceDescriptor, object>> scopes = new ConcurrentDictionary<HttpContext, ConcurrentDictionary<ServiceDescriptor, object>>();
        private readonly ConcurrentDictionary<HttpContext, List<IDisposable>> transients = new ConcurrentDictionary<HttpContext, List<IDisposable>>();
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

            if (!(service is null))
            {
                return GetService(service);
            }

            if (serviceType.IsGenericType)
            {
                if (descriptors.TryGetValue(serviceType, out var descriptor))
                {
                    return GetService(descriptor);
                }

                var genericType = serviceType.GetGenericTypeDefinition();

                var implementationService = serviceDescriptors.FirstOrDefault(x => x.ServiceType == genericType);

                if (implementationService is null || implementationService.ImplementationType is null)
                {
                    return null;
                }

                return GetService(descriptors.GetOrAdd(serviceType, implementationType =>
                {
                    return new ServiceDescriptor(implementationType, implementationService.ImplementationType.MakeGenericType(implementationType.GetGenericArguments()), implementationService.Lifetime);
                }));
            }

            if (serviceType.IsValueType || serviceType.IsInterface || serviceType.IsAbstract)
            {
                return null;
            }

            return GetService(descriptors.GetOrAdd(serviceType, implementationType =>
            {
                return new ServiceDescriptor(implementationType, implementationType, ServiceLifetime.Transient);
            }));
        }

        private object ImplementationInstance(ServiceDescriptor service)
        {
            if (service.ImplementationFactory is null)
            {
                service.ImplementationFactory = MakeImplementationFactory(service.ImplementationType);
            }

            return service.ImplementationFactory.Invoke(this);
        }

        private object GetService(ServiceDescriptor service)
        {
            switch (service.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    return service.ImplementationInstance ?? (service.ImplementationInstance = ImplementationInstance(service));
#if NET_NORMAL
                case ServiceLifetime.Scoped:
                    return service.ImplementationInstance ?? scopes.GetOrAdd(HttpContext.Current, context =>
                    {
                        context.AddOnRequestCompleted(context2 =>
                        {
                            if (scopes.TryRemove(context2, out ConcurrentDictionary<ServiceDescriptor, object> cache))
                            {
                                foreach (var kv in cache)
                                {
                                    if (kv.Value is IDisposable disposable)
                                    {
                                        disposable.Dispose();
                                    }
                                }

                                cache.Clear();
                            }
                        });

                        return new ConcurrentDictionary<ServiceDescriptor, object>();
                    }).GetOrAdd(service, ImplementationInstance);
#endif
                case ServiceLifetime.Transient when service.ImplementationInstance is null:

                    var instance = ImplementationInstance(service);

#if NET_NORMAL
                    if (instance is IDisposable disposableValue)
                    {
                        var results = transients.GetOrAdd(HttpContext.Current, context =>
                        {
                            context.AddOnRequestCompleted(context2 =>
                            {
                                if (transients.TryRemove(context2, out List<IDisposable> cache))
                                {
                                    foreach (var value in cache)
                                    {
                                        value.Dispose();
                                    }

                                    cache.Clear();
                                }
                            });

                            return new List<IDisposable>();
                        });

                        results.Add(disposableValue);
                    }
#endif

                    return instance;
                case ServiceLifetime.Transient:
                    return service.ImplementationInstance;
                default:
                    return null;
            }
        }

        private Func<IServiceProvider, object> MakeImplementationFactory(Type implementationType)
        {
            var constructors = implementationType
                     .GetConstructors();

            if (constructors.Length == 0)
            {
                throw new NotSupportedException($"类型“{implementationType.FullName}”不包含任何公共构造函数！");
            }

            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();

                if (parameters.Length == 0)
                {
                    return provider => constructor.Invoke(new object[0]);
                }

                if (parameters.All(x => serviceDescriptors.Any(y => x.ParameterType == y.ServiceType)))
                {
                    return provider => constructor.Invoke(parameters.Select(x => provider.GetService(x.ParameterType)).ToArray());
                }
            }

            throw new NotSupportedException($"类型“{implementationType.FullName}”所有公共构造函数都不被支持！");
        }
    }
}
#endif