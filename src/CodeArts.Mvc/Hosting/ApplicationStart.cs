#if NET40 || NET45 || NET451 || NET452 || NET461
using CodeArts.Mvc.Builder;
using CodeArts.Mvc.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Compilation;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.Dependencies;

[assembly: PreApplicationStartMethod(typeof(CodeArts.Mvc.Hosting.ApplicationStart), "Initialize")]

namespace CodeArts.Mvc.Hosting
{
    /// <summary>
    /// 程序启动
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ApplicationStart
    {
        private class ApplicationBuilder : IApplicationBuilder
        {
            private Func<RequestDelegate, RequestDelegate> _middleware = next => next;

            public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
            {
                if (middleware is null)
                {
                    throw new ArgumentNullException(nameof(middleware));
                }

                var prev = _middleware;

                _middleware = next =>
                {
                    return prev.Invoke(middleware(next));
                };

                return this;
            }

            public RequestDelegate Build(RequestDelegate request)
            {
                return _middleware.Invoke(request);
            }
        }
        private class DefaultResolver : IDependencyResolver, IDependencyScope, IDisposable
        {
            private readonly IServiceProvider provider;

            public IDependencyScope BeginScope() => this;

            public DefaultResolver(IServiceProvider provider)
            {
                this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            public object GetService(Type serviceType) => provider.GetService(serviceType);

            public IEnumerable<object> GetServices(Type serviceType) => Enumerable.Empty<object>();
        }
        private class MultiResolver : IDependencyResolver, IServiceProvider, IDependencyScope, IDisposable
        {
            private readonly IEnumerable<ServiceDescriptor> serviceDescriptors;
            private readonly IDependencyResolver dependencyResolver;
            private readonly ConcurrentDictionary<ServiceDescriptor, object> singletions = new ConcurrentDictionary<ServiceDescriptor, object>();
            private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> implementations = new ConcurrentDictionary<Type, Func<IServiceProvider, object>>();
            private readonly ConcurrentDictionary<Type, ServiceDescriptor> descriptors = new ConcurrentDictionary<Type, ServiceDescriptor>();
#if NET45 || NET451 || NET452 || NET461
            private readonly ConcurrentDictionary<HttpContext, ConcurrentDictionary<ServiceDescriptor, object>> scopes = new ConcurrentDictionary<HttpContext, ConcurrentDictionary<ServiceDescriptor, object>>();
#endif
            public MultiResolver(IEnumerable<ServiceDescriptor> serviceDescriptors, IDependencyResolver dependencyResolver)
            {
                this.serviceDescriptors = serviceDescriptors;
                this.dependencyResolver = dependencyResolver;
            }

            /// <summary>
            /// Gets the service object of the specified type.
            /// </summary>
            /// <param name="serviceType">服务类型</param>
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
                            return null;

                        descriptors.TryAdd(serviceType, descriptor = new ServiceDescriptor(serviceType, service.ImplementationType.MakeGenericType(serviceType.GetGenericArguments()), service.Lifetime));

                        return GetService(descriptor);
                    }

                    return dependencyResolver.GetService(serviceType);
                }

                return GetService(service);
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
#if NET45 || NET451 || NET452 || NET461
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

            public IDependencyScope BeginScope() => this;

            public IEnumerable<object> GetServices(Type serviceType) => Enumerable.Empty<object>();

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }
        }

        private class Loader
        {
            /// <summary>
            /// 启动类
            /// </summary>
            public const string Startup = "Startup";
            public const string Configuration = "Configuration";
            public const string ConfigureServices = "ConfigureServices";
            public const string Configure = "Configure";

            private readonly IEnumerable<Assembly> _referencedAssemblies;

            /// <summary>
            /// 构造函数
            /// </summary>
            public Loader() : this(null)
            {
            }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="referencedAssemblies">程序集</param>
            public Loader(IEnumerable<Assembly> referencedAssemblies)
            {
                _referencedAssemblies = referencedAssemblies ?? new ReferencedAssembliesWrapper();
            }

            private static bool AssemblyEquels(Assembly assembly, string fullName)
            {
                if (fullName.IndexOf(',') > -1)
                {
                    return assembly.FullName == fullName;
                }

                var assemblyName = assembly.FullName.Substring(0, assembly.FullName.IndexOf(','));

                return assemblyName.TrimEnd() == fullName;
            }

            /// <summary>
            /// 加载函数
            /// </summary>
            /// <returns></returns>
            public Func<HttpConfiguration, ApplicationBuilder> Load()
            {
                Type conversionType = Find(Startup);

                if (conversionType is null)
                    return null;

                var configuration = conversionType.GetMethod(Configuration, new Type[1] { typeof(HttpConfiguration) });

                if (configuration is null)
                    throw new EntryPointNotFoundException($"未找到配置启动的方法{Configuration}({typeof(HttpConfiguration)})。");

                var configureServices = conversionType.GetMethod(ConfigureServices, new Type[1] { typeof(IServiceCollection) });

                var configure = conversionType.GetMethod(Configure, new Type[1] { typeof(IApplicationBuilder) });

                if (configuration.ReturnType == typeof(void) || typeof(IServiceProvider).IsAssignableFrom(configuration.ReturnType) || typeof(IDependencyResolver).IsAssignableFrom(configuration.ReturnType))
                {
                    return config =>
                    {
                        var constructor = conversionType.GetConstructor(new Type[] { typeof(HttpConfiguration) });

                        var instance = constructor is null ? Activator.CreateInstance(conversionType, true) : constructor.Invoke(new object[1] { config });

                        if (configuration.ReturnType == typeof(void))
                        {
                            configuration.Invoke(instance, new object[1] { config });
                        }
                        else if (typeof(IServiceProvider).IsAssignableFrom(configuration.ReturnType))
                        {
                            IServiceProvider serviceProvider = (IServiceProvider)configuration.Invoke(instance, new object[1] { config });

                            config.DependencyResolver = new DefaultResolver(serviceProvider);
                        }
                        else
                        {
                            config.DependencyResolver = (IDependencyResolver)configuration.Invoke(instance, new object[1] { config });
                        }

                        if (!(configureServices is null))
                        {
                            IServiceCollection services = new ServiceCollection();

                            configureServices.Invoke(instance, new object[1] { services });

                            if (services.Count > 0)
                            {
                                var resolverType = config.DependencyResolver.GetType();

                                if (resolverType.Name == "EmptyResolver" && AssemblyEquels(resolverType.Assembly, "System.Web.Http"))
                                {
                                    config.DependencyResolver = new DefaultResolver(new ServiceProvider(services));
                                }
                                else
                                {
                                    config.DependencyResolver = new MultiResolver(services, config.DependencyResolver);
                                }
                            }
                        }

                        if (configure is null)
                            return null;

                        var handler = new ApplicationBuilder();

                        configure.Invoke(instance, new object[1] { handler });

                        return handler;
                    };
                }

                throw new NotSupportedException($"函数{Configuration}的返回值{configuration.ReturnType}类型不被支持!");
            }

            /// <summary>
            /// 查找类型
            /// </summary>
            /// <param name="typeName">类型名称</param>
            /// <returns></returns>
            private Type Find(string typeName)
            {
                bool conflict = false;
                Type matchedType = null;
                foreach (var assembly in _referencedAssemblies)
                {
                    // Startup
                    CheckForProgramType(typeName, assembly, ref matchedType, ref conflict);

                    // [AssemblyName].Startup
                    CheckForProgramType(assembly.GetName().Name + "." + typeName, assembly, ref matchedType, ref conflict);
                }

                if (matchedType == null)
                {
                    return null;
                }

                if (conflict)
                {
                    return null;
                }

                return matchedType;
            }

            private static void CheckForProgramType(string startupName, Assembly assembly, ref Type matchedType, ref bool conflict)
            {
                Type startupType = assembly.GetType(startupName, throwOnError: false);

                if (startupType != null)
                {
                    // Conflict?
                    if (matchedType != null)
                    {
                        conflict = true;
                    }
                    else
                    {
                        matchedType = startupType;
                    }
                }
            }

            private class ReferencedAssembliesWrapper : IEnumerable<Assembly>
            {
                public IEnumerator<Assembly> GetEnumerator()
                {
                    return BuildManager.GetReferencedAssemblies().Cast<Assembly>().GetEnumerator();
                }

                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }
            }
        }

        private sealed class PipelineBlueprint
        {
            public PipelineBlueprint(HttpConfiguration configuration, ApplicationBuilder builder)
            {
                Configuration = configuration;
                Builder = builder;
            }

            public HttpConfiguration Configuration { get; }
            public ApplicationBuilder Builder { get; }
        }

        private sealed class PipelineContext
        {
            private class AsyncResult : IAsyncResult
            {
                private readonly AsyncCallback _callback;
                private int _completions;

                public AsyncResult(AsyncCallback callback, object extradata)
                {
                    _callback = callback;
                    AsyncState = extradata;
                }

                public WaitHandle AsyncWaitHandle
                {
                    get { throw new NotImplementedException(); }
                }

                public bool IsCompleted { get; private set; }

                public object AsyncState { get; private set; }

                public bool CompletedSynchronously => false;

                public void TryComplete()
                {
                    if (!(_callback is null) && Interlocked.Increment(ref _completions) == 1)
                    {
                        IsCompleted = true;
                        _callback.Invoke(this);
                    }
                }
            }

            private readonly PipelineBlueprint pipeline;

            public PipelineContext(PipelineBlueprint pipeline)
            {
                this.pipeline = pipeline;
            }

            public void Initialize(HttpApplication context)
            {
#if !NET40
                pipeline.Configuration.EnsureInitialized();
#endif

                if (!(pipeline.Builder is null))
                {
                    context.AddOnAuthorizeRequestAsync(BeginEvent, EndEvent);
                }
            }
#if NET40
            private IAsyncResult BeginEvent(object sender, EventArgs e, AsyncCallback cb, object extraData)
            {
                var result = new AsyncResult(cb, extraData);

                var request = pipeline.Builder.Build(context => result.TryComplete());

                var appliction = (HttpApplication)sender;

                request.Invoke(appliction.Context);

                if (!result.IsCompleted)
                {
                    result.TryComplete();

                    appliction.CompleteRequest();
                }

                return result;
            }
#else
            private async Task<IAsyncResult> BeginEvent(object sender, EventArgs e, AsyncCallback cb, object extraData)
            {
                var result = new AsyncResult(cb, extraData);

                var request = pipeline.Builder.Build(context => Task.Run(() => result.TryComplete()));

                var appliction = (HttpApplication)sender;

                await request.Invoke(appliction.Context);

                if (!result.IsCompleted)
                {
                    result.TryComplete();

                    appliction.CompleteRequest();
                }

                return result;
            }
#endif
            private void EndEvent(IAsyncResult result)
            {
                if (!result.IsCompleted)
                {
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// 请求板块
        /// </summary>
        private sealed class HttpModule : IHttpModule
        {
            private static PipelineBlueprint _blueprint;
            private static bool _blueprintInitialized;
            private static object _blueprintLock = new object();

            public void Init(HttpApplication context)
            {
                PipelineBlueprint blueprint = LazyInitializer.EnsureInitialized(
                        ref _blueprint,
                        ref _blueprintInitialized,
                        ref _blueprintLock,
                        InitializeBlueprint);

                if (!(blueprint is null))
                {
                    PipelineContext pipeline = new PipelineContext(blueprint);

                    pipeline.Initialize(context);
                }
            }
            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }
            private PipelineBlueprint InitializeBlueprint()
            {
                var action = new Loader().Load();

                if (action is null)
                    return null;

                var config = GlobalConfiguration.Configuration;

                var builder = action.Invoke(config);

                return new PipelineBlueprint(config, builder);
            }
        }

        /// <summary>
        /// Registers the OWIN request processing module.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Initialize must never throw on server startup path")]
        public static void Initialize()
        {
#if NET40

            var _dynamicModuleRegistryField = typeof(HttpApplication).GetField("_dynamicModuleRegistry", BindingFlags.Static | BindingFlags.NonPublic);

            if (_dynamicModuleRegistryField is null)
                return;

            var _dynamicModuleRegistry = _dynamicModuleRegistryField.GetValue(null);

            if (_dynamicModuleRegistry is null)
                return;

            var moduleType = _dynamicModuleRegistry.GetType();

            var addMethod = moduleType.GetMethod("Add", new Type[] { typeof(Type) });

            addMethod.Invoke(_dynamicModuleRegistry, new object[] { typeof(HttpModule) });
#else
            HttpApplication.RegisterModule(typeof(HttpModule));
#endif
        }

        private const string _moduleNameFormat = "__DynamicModule_{0}_{1}";
        private static string MakeUniqueModuleName(Type moduleType)
        {
            // returns a unique name for this module
            return string.Format(CultureInfo.InvariantCulture, _moduleNameFormat, moduleType.AssemblyQualifiedName, Guid.NewGuid());
        }
    }
}
#endif