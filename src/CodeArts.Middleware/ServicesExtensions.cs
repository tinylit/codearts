using CodeArts.Emit;
using CodeArts.Proxies;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CodeArts
{
    /// <summary>
    /// 服务扩展。
    /// </summary>
    public static class ServicesExtensions
    {
        private static readonly Type InterceptAttributeType = typeof(InterceptAttribute);

        /// <summary>
        /// 使用拦截器。
        /// 为标记了 <see cref="InterceptAttribute"/> 的接口、类或方法生成代理类。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns></returns>
        public static IServiceCollection UseMiddleware(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var moduleEmitter = new ModuleEmitter();

            for (int i = 0; i < services.Count; i++)
            {
                ServiceDescriptor descriptor = services[i];

                if (!Intercept(descriptor))
                {
                    continue;
                }

                IProxyByPattern byPattern;

                if (descriptor.ImplementationType is null)
                {
                    if (descriptor.ImplementationInstance is null)
                    {
                        byPattern = new ProxyByInstance(moduleEmitter, descriptor.ServiceType, descriptor.ImplementationInstance);
                    }
                    else
                    {
                        byPattern = new ProxyByFactory(moduleEmitter, descriptor.ServiceType, descriptor.ImplementationFactory, descriptor.Lifetime);
                    }
                }
                else
                {
                    byPattern = new ProxyByImplementationType(moduleEmitter, descriptor.ServiceType, descriptor.ImplementationType, descriptor.Lifetime);

                }

                services[i] = byPattern.Resolve();
            }

            return services;
        }

        private static bool Intercept(ServiceDescriptor descriptor)
        {
            if (descriptor.ServiceType.IsDefined(InterceptAttributeType, true))
            {
                return true;
            }

            foreach (var methodInfo in descriptor.ServiceType.GetMethods())
            {
                if (methodInfo.IsDefined(InterceptAttributeType, true))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
