#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.AspNetCore.Mvc;
using SkyBuilding;
#else
using System.Web.Http;
#endif
using System;
using System.Linq;

#if NETSTANDARD2_0 || NETCOREAPP3_1
namespace Microsoft.Extensions.DependencyInjection
#else
namespace SkyBuilding.Mvc.DependencyInjection
#endif
{
    /// <summary>
    /// 服务集合扩展
    /// </summary>
    public static class DependencyInjectionServiceCollectionExtentions
    {
#if NETSTANDARD2_0 || NETCOREAPP3_1
        /// <summary>
        /// 使用依赖注入（注入继承<see cref="ControllerBase"/>的构造函数参数类型，也会注入【继承<see cref="ControllerBase"/>的构造函数参数】以及【其参数类型的构造函数参数】）
        /// </summary>
#else
        /// <summary>
        /// 使用依赖注入（注入继承<see cref="ApiController"/>的构造函数参数类型，也会注入【继承<see cref="ApiController"/>的构造函数参数】以及【其参数类型的构造函数参数】）
        /// </summary>
#endif
        public static IServiceCollection UseDependencyInjection(this IServiceCollection services)
        {
            var assemblys = AssemblyFinder.FindAll();

            var assemblyTypes = assemblys
                .SelectMany(x => x.GetTypes().Where(y => y.IsClass || y.IsInterface))
                .ToList();

#if NETSTANDARD2_0 || NETCOREAPP3_1
            var controllerTypes = assemblyTypes
                .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
                .ToList();
#else
            var controllerTypes = assemblyTypes
                .Where(type => !type.IsAbstract && typeof(ApiController).IsAssignableFrom(type))
                .ToList();
#endif

            var parameterTypes = controllerTypes
                .SelectMany(type =>
                {
                    return type.GetConstructors()
                    .Where(x => x.IsPublic)
                    .SelectMany(x => x.GetParameters().Select(y => y.ParameterType));

                })
                .Where(x => x.IsClass || x.IsInterface)
                .Distinct()
                .ToList();

            var types = parameterTypes
                .SelectMany(x => assemblyTypes.Where(y => y.IsClass && !y.IsAbstract && x.IsAssignableFrom(y)))
                .ToList();

            parameterTypes.ForEach(type =>
            {
                if (services.Any(x => x.ServiceType == type))
                    return;

                if (type.IsInterface || type.IsAbstract)
                {
                    foreach (var implementationType in types.Where(x => type.IsAssignableFrom(x)))
                    {
                        var ctor = implementationType.GetConstructors()
                        .Where(x => x.IsPublic)
                        .OrderBy(x => x.GetParameters().Length)
                        .FirstOrDefault() ?? throw new NotSupportedException($"类型“{implementationType.FullName}”不包含公共构造函数!");

                        var parameters = ctor.GetParameters();

                        if (parameters.Length == 0)
                        {
                            services.AddTransient(type, implementationType);

                            continue;
                        }

                        services.AddTransient(type, provider =>
                        {
                            return Activator.CreateInstance(implementationType, parameters.Select(x =>
                            {
                                var value = provider.GetService(x.ParameterType);

                                if (!(value is null)) return value;
#if NET40
                                if (x.DefaultValue != DBNull.Value)
#else
                                if (x.HasDefaultValue)
#endif
                                {
                                    return x.DefaultValue;
                                }

                                return Activator.CreateInstance(x.ParameterType);

                            }).ToArray());
                        });
                    }
                }
                else
                {
                    services.AddTransient(type);
                }
            });

            return services;
        }
    }
}