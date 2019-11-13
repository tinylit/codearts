using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extras.DynamicProxy;
using Autofac.Features.Scanning;
using Castle.DynamicProxy;
using System;
using System.Linq;

namespace SkyBuilding.AOP
{
    /// <summary>
    /// 将注册语法添加到Autofac.ContainerBuilder类型。
    /// </summary>
    public static class RegistrationExtensions
    {
        /// <summary>
        /// 注册AOP实例。(将注册所有标记了【InterceptAttribute】的接口和类，并且会自动注册构造函数 <see cref="InterceptAttribute(Type)"/> 的参数类型)
        /// </summary>
        /// <typeparam name="TImplementer">类型</typeparam>
        /// <param name="builder">构造器</param>
        /// <returns></returns>
        public static void RegisterInterceptors(this ContainerBuilder builder)
        {
            var assemblys = AssemblyFinder.FindAll();

            var assemblyTypes = assemblys
                .SelectMany(x => x.GetTypes().Where(y => y.IsClass || y.IsInterface))
                .ToList();

            var classTypes = assemblyTypes
                .Where(x => x.IsClass && Attribute.IsDefined(x, typeof(InterceptAttribute), false))
                .ToArray();

            builder.RegisterTypes(classTypes)
                .EnableClassInterceptors();

            var interfaceTypes = assemblyTypes
                .Where(x => x.IsInterface && Attribute.IsDefined(x, typeof(InterceptAttribute), false))
                .ToList();

            foreach (var interfaceType in interfaceTypes)
            {
                builder.RegisterTypes(assemblyTypes.Where(x => x.IsClass && !x.IsAbstract && interfaceType.IsAssignableFrom(x)).ToArray())
                    .As(interfaceType)
                    .EnableInterfaceInterceptors();
            }

            var typedTypes = interfaceTypes.Union(classTypes)
                .Select(x => Attribute.GetCustomAttribute(x, typeof(InterceptAttribute)))
                .Cast<InterceptAttribute>()
                .Where(x => x.InterceptorService is TypedService)
                .Select(x => x.InterceptorService)
                .Cast<TypedService>()
                .Select(x => x.ServiceType);

            builder.RegisterTypes(typedTypes.Distinct().ToArray());
        }

        /// <summary>
        /// 注册AOP实例。(将注册所有标记了【InterceptAttribute】的接口和类，并且会自动注册构造函数 <see cref="InterceptAttribute(Type)"/> 的参数类型)
        /// </summary>
        /// <typeparam name="TImplementer">类型</typeparam>
        /// <param name="builder">构造器</param>
        /// <param name="types">注册的类型集合</param>
        /// <returns></returns>
        public static void RegisterInterceptors(this ContainerBuilder builder, params Type[] types)
        {
            var classTypes = types
                .Where(x => x.IsClass && Attribute.IsDefined(x, typeof(InterceptAttribute), false))
                .ToArray();

            builder.RegisterTypes(classTypes)
                .EnableClassInterceptors();

            var interfaceTypes = types
                .Where(x => x.IsInterface && Attribute.IsDefined(x, typeof(InterceptAttribute), false))
                .ToList();

            foreach (var interfaceType in interfaceTypes)
            {
                builder.RegisterTypes(types.Where(x => x.IsClass && !x.IsAbstract && interfaceType.IsAssignableFrom(x)).ToArray())
                    .As(interfaceType)
                    .EnableInterfaceInterceptors();
            }

            var typedTypes = interfaceTypes.Union(classTypes)
                .Select(x => Attribute.GetCustomAttribute(x, typeof(InterceptAttribute)))
                .Cast<InterceptAttribute>()
                .Where(x => x.InterceptorService is TypedService)
                .Select(x => x.InterceptorService)
                .Cast<TypedService>()
                .Select(x => x.ServiceType);

            builder.RegisterTypes(typedTypes.Distinct().ToArray());
        }

        /// <summary>
        /// 在目标类型上启用接口拦截（通过类或接口上的拦截属性）。
        /// </summary>
        /// <typeparam name="TLimit">限制类型</typeparam>
        /// <typeparam name="TActivatorData">激活数据类型。</typeparam>
        /// <typeparam name="TSingleRegistrationStyle">注册的风格。</typeparam>
        /// <param name="registration">注册构造器</param>
        /// <returns></returns>
        public static IRegistrationBuilder<TLimit, TScanningActivatorData, TRegistrationStyle> EnableInterceptors<TLimit, TScanningActivatorData, TRegistrationStyle>(this IRegistrationBuilder<TLimit, TScanningActivatorData, TRegistrationStyle> registration) where TScanningActivatorData : ScanningActivatorData
        {
            registration.Where(x => x.IsClass)
                .EnableClassInterceptors();

            registration.Where(x => x.IsInterface)
                .EnableClassInterceptors();

            return registration;
        }
    }
    /// <summary>
    /// 默认注入器
    /// </summary>
    public class DefaultInterceptor : IInterceptor
    {
        /// <summary>
        /// 注入
        /// </summary>
        /// <param name="invocation"></param>
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
        }
    }
}
