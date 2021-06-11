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
        /// 为标记了 <see cref="InterceptAttribute"/> 的接口或方法生成代理类。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns></returns>
        public static IServiceCollection UseInterceptor(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            for (int i = 0; i < services.Count; i++)
            {
                ServiceDescriptor descriptor = services[i];

                if (IsIntercepted(descriptor.ServiceType))
                {
                    goto label_core;
                }

                if (descriptor.ImplementationType is null)
                {
                    continue;
                }

                if (IsIntercepted(descriptor.ImplementationType))
                {
                    goto label_implementation;
                }

                continue;

                label_core:
                {
                    if (descriptor.ImplementationType != null && IsIntercepted(descriptor.ImplementationType))
                    {
                        goto label_double;
                    }

                    continue;
                }

                label_double:
                {
                    MakeInterceptType(descriptor.ServiceType, descriptor.ImplementationType);

                    continue;
                }

                label_implementation:
                {
                    continue;
                }
            }

            return services;
        }

        /// <summary>
        /// 拦截到。
        /// </summary>
        /// <param name="interceptType">拦截类型。</param>
        /// <returns></returns>
        private static bool IsIntercepted(Type interceptType)
        {
            if (interceptType.IsDefined(InterceptAttributeType, true))
            {
                return true;
            }

            foreach (var methodInfo in interceptType.GetMethods())
            {
                if (methodInfo.IsDefined(InterceptAttributeType, true))
                {
                    return true;
                }
            }

            return false;
        }

        private static Type MakeInterceptType(Type serviceType, Type implementationType)
        {
            return null;
        }
    }
}
