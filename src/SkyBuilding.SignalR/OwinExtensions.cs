#if NET45 || NET451 || NET452 ||NET461
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security.DataProtection;
using Owin;
using SkyBuilding.SignalR.Owin.Middleware;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace SkyBuilding.SignalR
{
    /// <summary>
    /// Owin 扩展
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Owin", Justification = "The owin namespace is for consistentcy.")]
    public static class OwinExtensions
    {
        private static class MonoUtility
        {
            private static readonly Lazy<bool> _isRunningMono = new Lazy<bool>(() => CheckRunningOnMono());

            public static bool IsRunningMono
            {
                get
                {
                    return _isRunningMono.Value;
                }
            }

            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This should never fail")]
            private static bool CheckRunningOnMono()
            {
                try
                {
                    return Type.GetType("Mono.Runtime") != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Adds SignalR hubs to the app builder pipeline. Use <see cref="JwtHubDispatcherMiddleware"/> middleware.
        /// </summary>
        /// <param name="builder">The app builder</param>
        public static void UseJwtSignalR(this IAppBuilder builder)
        {
            builder.UseSignalRMiddleware<JwtHubDispatcherMiddleware>(new HubConfiguration());
        }

        /// <summary>
        /// Adds the specified SignalR <see cref="PersistentConnection"/> to the app builder. Use  <see cref="JwtPersistentConnectionMiddleware"/> middleware.
        /// </summary>
        /// <typeparam name="TConnection">The type of <see cref="PersistentConnection"/></typeparam>
        /// <param name="builder">The app builder</param>
        /// <param name="configuration">The <see cref="ConnectionConfiguration"/> to use</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is syntactic sugar")]
        public static void UseJwtSignalR<TConnection>(this IAppBuilder builder, ConnectionConfiguration configuration) where TConnection : PersistentConnection
        {
            builder.UseSignalR<JwtPersistentConnectionMiddleware>(configuration, typeof(TConnection));
        }

        /// <summary>
        /// Adds SignalR hubs to the app builder pipeline.
        /// </summary>
        /// <param name="builder">The app builder</param>
        public static void UseSignalR<TMiddleware>(this IAppBuilder builder) where TMiddleware : OwinMiddleware
        {
            builder.UseSignalRMiddleware<TMiddleware>(new HubConfiguration());
        }

        /// <summary>
        /// Adds SignalR hubs to the app builder pipeline.
        /// </summary>
        /// <param name="builder">The app builder</param>
        /// <param name="configuration">The <see cref="ConnectionConfiguration"/> to use</param>
        /// <param name="arguments">The Middleware constructor other parameters.</param>
        /// <example>New(OwinMiddleware next, HubConfiguration configuration,...arguments)</example>
        public static void UseSignalR<TMiddleware>(this IAppBuilder builder, ConnectionConfiguration configuration, params object[] arguments) where TMiddleware : OwinMiddleware
        {
            builder.UseSignalRMiddleware<TMiddleware>(configuration, arguments);
        }

        private static CancellationToken GetShutdownToken(this IDictionary<string, object> env)
        {
            return env.TryGetValue("host.OnAppDisposing", out object value)
                && value is CancellationToken
                ? (CancellationToken)value
                : default;
        }
        private static string GetAppInstanceName(this IDictionary<string, object> environment)
        {
            if (environment.TryGetValue("host.AppName", out object value))
            {
                var stringVal = value as string;

                if (!string.IsNullOrEmpty(stringVal))
                {
                    return stringVal;
                }
            }

            return null;
        }
        private static TextWriter GetTraceOutput(this IDictionary<string, object> environment)
        {
            if (environment.TryGetValue("host.TraceOutput", out object value))
            {
                return value as TextWriter;
            }

            return null;
        }

        private static IEnumerable<Assembly> GetReferenceAssemblies(this IDictionary<string, object> environment)
        {
            if (environment.TryGetValue("host.ReferencedAssemblies", out object assembliesValue))
            {
                return (IEnumerable<Assembly>)assembliesValue;
            }

            return null;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This class wires up new dependencies from the host")]
        private static IAppBuilder UseSignalRMiddleware<T>(this IAppBuilder builder, ConnectionConfiguration configuration, params object[] arguments)
        {
            if (configuration == null)
            {
                throw new ArgumentException("链接配置为空!");
            }

            var resolver = configuration.Resolver;

            if (resolver == null)
            {
                throw new ArgumentException("未实现依赖解决方案!");
            }

            EnsureValidCulture();

            // Ensure we have the conversions for MS.Owin so that
            // the app builder respects the OwinMiddleware base class
            SignatureConversions.AddConversions(builder);

            var env = builder.Properties;
            CancellationToken token = env.GetShutdownToken();

            // If we don't get a valid instance name then generate a random one
            string instanceName = env.GetAppInstanceName() ?? Guid.NewGuid().ToString();

            // Use the data protection provider from app builder and fallback to the
            // Dpapi provider
            IDataProtectionProvider provider = builder.GetDataProtectionProvider();
            IProtectedData protectedData;

            // If we're using DPAPI then fallback to the default protected data if running
            // on mono since it doesn't support any of this
            if (provider == null && MonoUtility.IsRunningMono)
            {
                protectedData = new DefaultProtectedData();
            }
            else
            {
                if (provider == null)
                {
                    provider = new DpapiDataProtectionProvider(instanceName);
                }

                protectedData = new DataProtectionProviderProtectedData(provider);
            }

            resolver.Register(typeof(IProtectedData), () => protectedData);

            // If the host provides trace output then add a default trace listener
            TextWriter traceOutput = env.GetTraceOutput();
            if (traceOutput != null)
            {
                var hostTraceListener = new TextWriterTraceListener(traceOutput);
                var traceManager = new TraceManager(hostTraceListener);
                resolver.Register(typeof(ITraceManager), () => traceManager);
            }

            // Try to get the list of reference assemblies from the host
            IEnumerable<Assembly> referenceAssemblies = env.GetReferenceAssemblies();
            if (referenceAssemblies != null)
            {
                // Use this list as the assembly locator
                var assemblyLocator = new EnumerableOfAssemblyLocator(referenceAssemblies);
                resolver.Register(typeof(IAssemblyLocator), () => assemblyLocator);
            }

            resolver.InitializeHost(instanceName, token);

            builder.Use(typeof(T), new object[1] { configuration }.Concat(arguments));

            // BUG 2306: We need to make that SignalR runs before any handlers are
            // mapped in the IIS pipeline so that we avoid side effects like
            // session being enabled. The session behavior can be
            // manually overridden if user calls SetSessionStateBehavior but that shouldn't
            // be a problem most of the time.
            builder.UseStageMarker(PipelineStage.PostAuthorize);

            return builder;
        }

        private static void EnsureValidCulture()
        {
            // The CultureInfo may leak across app domains which may cause hangs. The most prominent
            // case in SignalR are MapSignalR hangs when creating Performance Counters (#3414).
            // See https://github.com/SignalR/SignalR/issues/3414#issuecomment-152733194 for more details.
            var culture = CultureInfo.CurrentCulture;
            while (!culture.Equals(CultureInfo.InvariantCulture))
            {
                culture = culture.Parent;
            }

            if (ReferenceEquals(culture, CultureInfo.InvariantCulture))
            {
                return;
            }

            var thread = Thread.CurrentThread;
            thread.CurrentCulture = CultureInfo.GetCultureInfo(thread.CurrentCulture.Name);
            thread.CurrentUICulture = CultureInfo.GetCultureInfo(thread.CurrentUICulture.Name);
        }
    }
}
#endif