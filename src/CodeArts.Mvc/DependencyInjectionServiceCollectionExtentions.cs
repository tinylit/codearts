#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.AspNetCore.Mvc;
using CodeArts;
#else
using System.Web.Http;
#endif
using System;
using System.Linq;
using System.Collections.Generic;

#if NETSTANDARD2_0 || NETCOREAPP3_1
namespace Microsoft.Extensions.DependencyInjection
#else
namespace CodeArts.Mvc.DependencyInjection
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
        /// <remarks>可配置【di:pattern】缩小依赖注入程序集范围，作为<see cref="System.IO.Directory.GetFiles(string, string)"/>的第二个参数，默认:“*”。</remarks>
        /// <remarks>可配置【di:singleton】决定注入控制器构造函数时，是否参数单例模式注入，默认：true。</remarks>
#else
        /// <summary>
        /// 使用依赖注入（注入继承<see cref="ApiController"/>的构造函数参数类型，也会注入【继承<see cref="ApiController"/>的构造函数参数】以及【其参数类型的构造函数参数】）
        /// </summary>
        /// <remarks>可配置【di-pattern】缩小依赖注入程序集范围，作为<see cref="System.IO.Directory.GetFiles(string, string)"/>的第二个参数，默认:“*”。</remarks>
        /// <remarks>可配置【di-singleton】决定注入控制器构造函数时，是否参数单例模式注入，默认：true。</remarks>
#endif
        public static IServiceCollection UseDependencyInjection(this IServiceCollection services)
        {
#if NETSTANDARD2_0 || NETCOREAPP3_1
            var assemblys = AssemblyFinder.Find("di:pattern".Config("*"));
#else
            var assemblys = AssemblyFinder.Find("di-pattern".Config("*"));
#endif

            var assemblyTypes = assemblys
                .SelectMany(x => x.GetTypes().Where(y => y.IsClass || y.IsInterface))
                .ToList();

#if NETSTANDARD2_0 || NETCOREAPP3_1
            var controllerType = typeof(ControllerBase);
#else
            var controllerType = typeof(ApiController);
#endif

            var controllerTypes = assemblyTypes
                .Where(type => !type.IsAbstract && controllerType.IsAssignableFrom(type))
                .ToList();

            var parameterTypes = controllerTypes
                .SelectMany(type =>
                {
                    return type.GetConstructors()
                    .Where(x => x.IsPublic)
                    .SelectMany(x => x.GetParameters()
                        .Select(y => y.ParameterType)
                    );

                })
                .Where(x => x.IsClass || x.IsInterface)
                .Distinct()
                .ToList();

            var implementationTypes = parameterTypes
                .SelectMany(x => assemblyTypes.Where(y => y.IsClass && !y.IsAbstract && x.IsAssignableFrom(y)))
                .ToList();

            var dic = new Dictionary<Type, object>();

#if NETSTANDARD2_0 || NETCOREAPP3_1
            bool isSingleton = "di:singleton".Config(true);
#else
            bool isSingleton = "di-singleton".Config(true);
#endif
            assemblyTypes
            .Where(x => x.IsDefined(typeof(TypeGenAttribute), false))
            .ForEach(x =>
            {
                var attr = (TypeGenAttribute)Attribute.GetCustomAttribute(x, typeof(TypeGenAttribute), false);

                var implementationType = attr.TypeGen.Create(x);

                if (isSingleton)
                {
                    services.AddSingleton(x, implementationType);
                }
                else
                {
                    services.AddTransient(x, implementationType);
                }
            });

            parameterTypes
                .ForEach(parameterType =>
                {
                    if (services.Any(y => y.ServiceType == parameterType))
                    {
                        return;
                    }

                    if (!(parameterType.IsInterface || parameterType.IsAbstract))
                    {
                        services.AddTransient(parameterType);

                        return;
                    }

                    foreach (var implementationType in implementationTypes.Where(x => parameterType.IsAssignableFrom(x)))
                    {
                        implementationType
                       .GetConstructors()
                       .Where(x => x.IsPublic)
                       .SelectMany(x => x.GetParameters())
                       .ForEach(x =>
                       {
                           var serviceType = x.ParameterType;

                           if (services.Any(y => y.ServiceType == serviceType))
                           {
                               return;
                           }

                           if (x.ParameterType.IsInterface || x.ParameterType.IsAbstract)
                           {
                               foreach (var item in assemblyTypes.Where(y => y.IsClass && !y.IsAbstract && serviceType.IsAssignableFrom(y)))
                               {
                                   if (isSingleton)
                                   {
                                       services.AddSingleton(serviceType, item);
                                   }
                                   else
                                   {
                                       services.AddTransient(serviceType, item);
                                   }

                                   break;
                               }
                           }
                           else if (isSingleton)
                           {
                               services.AddSingleton(serviceType);
                           }
                           else
                           {
                               services.AddTransient(serviceType);
                           }
                       });

                        services.AddTransient(parameterType, implementationType);

                        break;
                    }
                });

            return services;
        }
    }
}