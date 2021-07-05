using CodeArts.Proxies;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

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
        public static IServiceCollection UseAOP(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            for (int i = 0; i < services.Count; i++)
            {
                ServiceDescriptor descriptor = services[i];

                if (!Intercept(descriptor))
                {
                    continue;
                }

                IProxyByPattern byPattern;

                if (descriptor.ImplementationInstance is null)
                {
                    if (descriptor.ImplementationType is null)
                    {
                        byPattern = new ProxyByFactory(descriptor.ServiceType, descriptor.ImplementationFactory, descriptor.Lifetime);
                    }
                    else
                    {
                        byPattern = new ProxyByType(descriptor.ServiceType, descriptor.ImplementationType, descriptor.Lifetime);

                    }
                }
                else
                {
                    byPattern = new ProxyByInstance(descriptor.ServiceType, descriptor.ImplementationInstance);
                }

                services[i] = byPattern.Resolve();
            }

            return services;
        }

        private static bool Intercept(ServiceDescriptor descriptor)
        {
            bool inherit = descriptor.ServiceType.IsInterface;

            if (descriptor.ServiceType.IsDefined(InterceptAttributeType, inherit))
            {
                return true;
            }

            var implementationType = descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();

            if (implementationType is null || implementationType.IsNested)
            {
                goto label_methods;
            }

            if (implementationType.IsDefined(InterceptAttributeType, false))
            {
                return true;
            }

            foreach (var methodInfo in implementationType.GetMethods())
            {
                if (methodInfo.IsDefined(InterceptAttributeType, false))
                {
                    return true;
                }
            }

            label_methods:

            foreach (var methodInfo in descriptor.ServiceType.GetMethods())
            {
                if (methodInfo.IsDefined(InterceptAttributeType, inherit))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
