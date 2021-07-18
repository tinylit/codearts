#if NET40_OR_GREATER
using CodeArts.Mvc.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
#if NET40
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
using System.Reflection;
using System.Threading;
#if NET45_OR_GREATER
using System.Threading.Tasks;
#endif
using System.Web;
using System.Web.Compilation;
using System.Web.Http;
using System.Web.Http.Dependencies;

[assembly: PreApplicationStartMethod(typeof(CodeArts.Mvc.Hosting.ApplicationStart), "Initialize")]

namespace CodeArts.Mvc.Hosting
{
    /// <summary>
    /// 程序启动。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ApplicationStart
    {
        private static readonly Type ResolverType = typeof(IDependencyResolver);
        private static readonly Type ScopeType = typeof(IDependencyScope);
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

        private class DefaultScope : IDependencyScope, IDisposable
        {
            private readonly IServiceScope serviceScope;

            public DefaultScope(IServiceScope serviceScope)
            {
                this.serviceScope = serviceScope;
            }

            public void Dispose() => serviceScope.Dispose();

            public object GetService(Type serviceType)
            {
                var serviceObj = serviceScope.ServiceProvider.GetService(serviceType);

                if (serviceObj is null)
                {
                    if (serviceType == ResolverType)
                    {
                        return new DefaultResolver(serviceScope.ServiceProvider);
                    }

                    if (serviceType == ScopeType)
                    {
                        return this;
                    }
                }

                return serviceObj;
            }

            public IEnumerable<object> GetServices(Type serviceType) => serviceScope.ServiceProvider.GetServices(serviceType);
        }
        private class DefaultResolver : IDependencyResolver, IDependencyScope, IDisposable
        {
            private readonly IServiceProvider provider;
            public IDependencyScope BeginScope() => new DefaultScope(provider.CreateScope());

            public DefaultResolver(IServiceProvider provider)
            {
                this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            }

            public void Dispose() => GC.SuppressFinalize(this);

            public object GetService(Type serviceType)
            {
                var serviceObj = provider.GetService(serviceType);

                if (serviceObj is null)
                {
                    if (serviceType == ResolverType)
                    {
                        return this;
                    }

                    if (serviceType == ScopeType)
                    {
                        return this;
                    }
                }

                return serviceObj;
            }

            public IEnumerable<object> GetServices(Type serviceType) => provider.GetServices(serviceType);
        }

        private class Loader
        {
            /// <summary>
            /// 启动类。
            /// </summary>
            public const string Startup = "Startup";
            public const string Configuration = "Configuration";
            public const string ConfigureServices = "ConfigureServices";
            public const string Configure = "Configure";

            private readonly IEnumerable<Assembly> _referencedAssemblies;

            /// <summary>
            /// 构造函数。
            /// </summary>
            public Loader() : this(null)
            {
            }

            /// <summary>
            /// 构造函数。
            /// </summary>
            /// <param name="referencedAssemblies">程序集。</param>
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
            /// 加载函数。
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
                            var controllerBaseType = typeof(ApiController);

                            IServiceCollection services = new ServiceCollection();

                            configureServices.Invoke(instance, new object[1] { services });

                            foreach (var controllerType in _referencedAssemblies
                            .SelectMany(x => x.GetTypes())
                            .Where(x => x.IsClass && !x.IsAbstract && controllerBaseType.IsAssignableFrom(x)))
                            {
                                services.AddTransient(controllerType);
                            }

                            if (services.Count > 0)
                            {
                                var resolverType = config.DependencyResolver.GetType();

                                if (resolverType.Name == "EmptyResolver" && AssemblyEquels(resolverType.Assembly, "System.Web.Http"))
                                {
                                    config.DependencyResolver = new DefaultResolver(services.BuildServiceProvider());
                                }
                                else
                                {
                                    throw new NotSupportedException("不支持多次定义“HttpConfiguration.DependencyResolver”的实现!");
                                }
                            }
                        }

                        var methodInfos = conversionType.GetMethods();

                        foreach (var configure in methodInfos)
                        {
                            if (!string.Equals(configure.Name, Configure) || configure.DeclaringType != conversionType)
                            {
                                continue;
                            }

                            return Done(instance, config, configure);
                        }

                        foreach (var configure in methodInfos)
                        {
                            if (!string.Equals(configure.Name, Configure))
                            {
                                continue;
                            }

                            return Done(instance, config, configure);
                        }

                        return null;
                    };
                }

                ApplicationBuilder Done(object instance, HttpConfiguration config, MethodInfo configure)
                {
                    if (configure.ReturnType != typeof(void))
                    {
                        throw new NotSupportedException($"函数“{configure.Name}”必须是无返回值类型!");
                    }

                    var applicationBuilderType = typeof(IApplicationBuilder);

                    var parameters = configure.GetParameters();

                    if (!parameters.Any(x => x.ParameterType == applicationBuilderType))
                    {
                        configure.Invoke(instance, parameters
                            .Select(x => config.DependencyResolver.GetService(x.ParameterType))
                            .ToArray());

                        return null;
                    }

                    var handler = new ApplicationBuilder();

                    configure.Invoke(instance, parameters
                    .Select(x =>
                    {
                        if (x.ParameterType == applicationBuilderType)
                        {
                            return handler;
                        }

                        return config.DependencyResolver.GetService(x.ParameterType);
                    })
                    .ToArray());

                    return handler;
                }

                throw new NotSupportedException($"函数“{Configuration}”的返回值{configuration.ReturnType}类型不被支持!");
            }

            /// <summary>
            /// 查找类型。
            /// </summary>
            /// <param name="typeName">类型名称。</param>
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

                if (matchedType is null)
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
#if NET45_OR_GREATER
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

                await request.Invoke(appliction.Context).ConfigureAwait(false);

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
        /// 请求板块。
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
        public static void Initialize()
        {
#if NET40

            var applicationType = typeof(HttpApplication);

            var _dynamicModuleRegistryField = applicationType
                .GetField("_dynamicModuleRegistry", BindingFlags.Static | BindingFlags.NonPublic) ?? applicationType
                .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                    .FirstOrDefault(x => x.FieldType.FullName.StartsWith("System.Web.DynamicModuleRegistry"));

            if (_dynamicModuleRegistryField is null)
            {
                return;
            }

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
    }
}
#endif