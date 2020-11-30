#if NET40 || NET_NORMAL
using System;
using System.Diagnostics;

namespace CodeArts.Mvc.DependencyInjection
{
    /// <summary>
    /// Describes a service with its service type, implementation, and lifetime.
    /// </summary>
    [DebuggerDisplay("Lifetime = {Lifetime}, ServiceType = {ServiceType}, ImplementationType = {ImplementationType}")]
    public class ServiceDescriptor
    {
        /// <inheritdoc />
        public ServiceLifetime Lifetime
        {
            get;
        }

        /// <inheritdoc />
        public Type ServiceType
        {
            get;
        }

        /// <inheritdoc />
        public Type ImplementationType
        {
            get;
        }

        /// <inheritdoc />
        public object ImplementationInstance
        {
            get;
        }

        /// <inheritdoc />
        public Func<IServiceProvider, object> ImplementationFactory
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDescriptor" /> with the specified <paramref name="implementationType" />.
        /// </summary>
        /// <param name="serviceType">The <see cref="T:System.Type" /> of the service.</param>
        /// <param name="implementationType">The <see cref="T:System.Type" /> implementing the service.</param>
        /// <param name="lifetime">The <see cref="ServiceLifetime" /> of the service.</param>
        public ServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
            : this(serviceType, lifetime)
        {
            if (serviceType is null)
            {
                throw new ArgumentNullException("serviceType");
            }

            ImplementationType = implementationType ?? throw new ArgumentNullException("implementationType");
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDescriptor" /> with the specified <paramref name="instance" />
        /// as a <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton" />.
        /// </summary>
        /// <param name="serviceType">The <see cref="T:System.Type" /> of the service.</param>
        /// <param name="instance">The instance implementing the service.</param>
        public ServiceDescriptor(Type serviceType, object instance)
            : this(serviceType, ServiceLifetime.Singleton)
        {
            if (serviceType is null)
            {
                throw new ArgumentNullException("serviceType");
            }

            ImplementationInstance = instance ?? throw new ArgumentNullException("instance");
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceDescriptor" /> with the specified <paramref name="factory" />.
        /// </summary>
        /// <param name="serviceType">The <see cref="T:System.Type" /> of the service.</param>
        /// <param name="factory">A factory used for creating service instances.</param>
        /// <param name="lifetime">The <see cref="ServiceLifetime" /> of the service.</param>
        public ServiceDescriptor(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
            : this(serviceType, lifetime)
        {
            if (serviceType is null)
            {
                throw new ArgumentNullException("serviceType");
            }

            ImplementationFactory = factory ?? throw new ArgumentNullException("factory");
        }

        private ServiceDescriptor(Type serviceType, ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
            ServiceType = serviceType;
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <typeparamref name="TService" />, <typeparamref name="TImplementation" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient" /> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Transient<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            return Describe<TService, TImplementation>(ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <paramref name="service" /> and <paramref name="implementationType" />
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient" /> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="implementationType">The type of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Transient(Type service, Type implementationType)
        {
            if (service is null)
            {
                throw new ArgumentNullException("service");
            }
            if (implementationType is null)
            {
                throw new ArgumentNullException("implementationType");
            }
            return Describe(service, implementationType, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <typeparamref name="TService" />, <typeparamref name="TImplementation" />,
        /// <paramref name="implementationFactory" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient" /> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Transient<TService, TImplementation>(Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService
        {
            if (implementationFactory is null)
            {
                throw new ArgumentNullException("implementationFactory");
            }
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <typeparamref name="TService" />, <paramref name="implementationFactory" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient" /> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Transient<TService>(Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            if (implementationFactory is null)
            {
                throw new ArgumentNullException("implementationFactory");
            }
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Transient);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <paramref name="service" />, <paramref name="implementationFactory" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient" /> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Transient(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            if (service is null)
            {
                throw new ArgumentNullException("service");
            }
            if (implementationFactory is null)
            {
                throw new ArgumentNullException("implementationFactory");
            }
            return Describe(service, implementationFactory, ServiceLifetime.Transient);
        }
#if NET_NORMAL
        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <typeparamref name="TService" />, <typeparamref name="TImplementation" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped" /> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Scoped<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            return Describe<TService, TImplementation>(ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <paramref name="service" /> and <paramref name="implementationType" />
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped" /> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="implementationType">The type of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Scoped(Type service, Type implementationType)
        {
            return Describe(service, implementationType, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <typeparamref name="TService" />, <typeparamref name="TImplementation" />,
        /// <paramref name="implementationFactory" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped" /> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Scoped<TService, TImplementation>(Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService
        {
            if (implementationFactory is null)
            {
                throw new ArgumentNullException("implementationFactory");
            }
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <typeparamref name="TService" />, <paramref name="implementationFactory" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped" /> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Scoped<TService>(Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            if (implementationFactory is null)
            {
                throw new ArgumentNullException("implementationFactory");
            }
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Scoped);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <paramref name="service" />, <paramref name="implementationFactory" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped" /> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Scoped(Type service, Func<IServiceProvider, object> implementationFactory)
        {
            if (service is null)
            {
                throw new ArgumentNullException("service");
            }
            if (implementationFactory is null)
            {
                throw new ArgumentNullException("implementationFactory");
            }
            return Describe(service, implementationFactory, ServiceLifetime.Scoped);
        }
#endif
        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <typeparamref name="TService" />, <typeparamref name="TImplementation" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton" /> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Singleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            return Describe<TService, TImplementation>(ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <paramref name="service" /> and <paramref name="implementationType" />
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton" /> lifetime.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="implementationType">The type of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Singleton(Type service, Type implementationType)
        {
            if (service is null)
            {
                throw new ArgumentNullException("service");
            }
            if (implementationType is null)
            {
                throw new ArgumentNullException("implementationType");
            }
            return Describe(service, implementationType, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <typeparamref name="TService" />, <typeparamref name="TImplementation" />,
        /// <paramref name="implementationFactory" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton" /> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Singleton<TService, TImplementation>(Func<IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService
        {
            if (implementationFactory is null)
            {
                throw new ArgumentNullException("implementationFactory");
            }
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <typeparamref name="TService" />, <paramref name="implementationFactory" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton" /> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Singleton<TService>(Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            if (implementationFactory is null)
            {
                throw new ArgumentNullException("implementationFactory");
            }
            return Describe(typeof(TService), implementationFactory, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <paramref name="serviceType" />, <paramref name="implementationFactory" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton" /> lifetime.
        /// </summary>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Singleton(Type serviceType, Func<IServiceProvider, object> implementationFactory)
        {
            if (serviceType is null)
            {
                throw new ArgumentNullException("serviceType");
            }
            if (implementationFactory is null)
            {
                throw new ArgumentNullException("implementationFactory");
            }
            return Describe(serviceType, implementationFactory, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <typeparamref name="TService" />, <paramref name="implementationInstance" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped" /> lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="implementationInstance">The instance of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Singleton<TService>(TService implementationInstance) where TService : class
        {
            if (implementationInstance is null)
            {
                throw new ArgumentNullException("implementationInstance");
            }
            return Singleton(typeof(TService), implementationInstance);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <paramref name="serviceType" />, <paramref name="implementationInstance" />,
        /// and the <see cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped" /> lifetime.
        /// </summary>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="implementationInstance">The instance of the implementation.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Singleton(Type serviceType, object implementationInstance)
        {
            if (serviceType is null)
            {
                throw new ArgumentNullException("serviceType");
            }
            if (implementationInstance is null)
            {
                throw new ArgumentNullException("implementationInstance");
            }
            return new ServiceDescriptor(serviceType, implementationInstance);
        }

        private static ServiceDescriptor Describe<TService, TImplementation>(ServiceLifetime lifetime) where TService : class where TImplementation : class, TService
        {
            return Describe(typeof(TService), typeof(TImplementation), lifetime);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <paramref name="serviceType" />, <paramref name="implementationType" />,
        /// and <paramref name="lifetime" />.
        /// </summary>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="implementationType">The type of the implementation.</param>
        /// <param name="lifetime">The lifetime of the service.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Describe(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(serviceType, implementationType, lifetime);
        }

        /// <summary>
        /// Creates an instance of <see cref="ServiceDescriptor" /> with the specified
        /// <paramref name="serviceType" />, <paramref name="implementationFactory" />,
        /// and <paramref name="lifetime" />.
        /// </summary>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="implementationFactory">A factory to create new instances of the service implementation.</param>
        /// <param name="lifetime">The lifetime of the service.</param>
        /// <returns>A new instance of <see cref="ServiceDescriptor" />.</returns>
        public static ServiceDescriptor Describe(Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime)
        {
            return new ServiceDescriptor(serviceType, implementationFactory, lifetime);
        }
    }
}
#endif