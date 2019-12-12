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
        /// 使用依赖注入（注入继承<see cref="ControllerBase"/>的构造函数参数类型，若引入了【SkyBuilding.ORM】，将会注入【继承<see cref="ControllerBase"/>的构造函数参数】以及【其参数类型的构造函数参数】中使用到的【数据仓库类型】）
        /// </summary>
#else
        /// <summary>
        /// 使用依赖注入（注入继承<see cref="ApiController"/>的构造函数参数类型，若引入了【SkyBuilding.ORM】，将会注入【继承<see cref="ApiController"/>的构造函数参数】以及【其参数类型的构造函数参数】中使用到的【数据仓库类型】）
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

            var repositoryTypes = assemblys.Where(x => x.FullName.StartsWith("SkyBuilding.ORM"))
                .SelectMany(x => x.GetTypes().Where(y => y.IsClass && y.IsAbstract && y.FullName == "SkyBuilding.ORM.Repository"))
                .ToList();

            if (repositoryTypes.Count > 0)
            {
                var injectionTypes = types.SelectMany(type =>
                {
                    return type.GetConstructors()
                        .Where(x => x.IsPublic)
                        .SelectMany(x => x.GetParameters().Select(y => y.ParameterType))
                        .Where(x => (x.IsClass || x.IsInterface) && repositoryTypes.Any(y => y.IsAssignableFrom(x)));
                }).ToList();

                var exactlyTypes = assemblyTypes
                    .Where(type => type.IsClass && injectionTypes.Any(x => x.IsAssignableFrom(type)))
                    .ToList();

                exactlyTypes.ForEach(type =>
                {
                    services.AddSingleton(type);

                    foreach (Type interfaceType in type.GetInterfaces())
                    {
                        services.AddSingleton(interfaceType, type);
                    }
                });
            }

            parameterTypes.ForEach(type =>
            {
                if (type.IsInterface || type.IsAbstract)
                {
                    foreach (var implementationType in types.Where(x => type.IsAssignableFrom(x)))
                    {
                        services.AddTransient(type, implementationType);

                        break;
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