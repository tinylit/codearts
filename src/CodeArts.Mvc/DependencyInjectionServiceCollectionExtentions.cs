#if NET_CORE
using Microsoft.AspNetCore.Mvc;
using CodeArts;
#else
using System.Web.Http;
#endif
using System;
using System.Linq;
using System.Collections.Generic;

#if NET_CORE
namespace Microsoft.Extensions.DependencyInjection
#else
namespace CodeArts.Mvc.DependencyInjection
#endif
{
    /// <summary>
    /// 服务集合扩展。
    /// </summary>
    public static class DependencyInjectionServiceCollectionExtentions
    {
#if NET_CORE
        /// <summary>
        /// 使用依赖注入（注入继承<see cref="ControllerBase"/>的构造函数参数类型，也会注入【继承<see cref="ControllerBase"/>的构造函数参数】以及【其参数类型的构造函数参数】）。
        /// </summary>
        /// <remarks>可配置【di:pattern】缩小依赖注入程序集范围，作为<see cref="System.IO.Directory.GetFiles(string, string)"/>的第二个参数，默认:“*”。</remarks>
        /// <remarks>可配置【di:singleton】决定注入控制器构造函数时，是否参数单例模式注入，默认：true。</remarks>
#else
        /// <summary>
        /// 使用依赖注入（注入继承<see cref="ApiController"/>的构造函数参数类型，也会注入【继承<see cref="ApiController"/>的构造函数参数】以及【其参数类型的构造函数参数】）。
        /// </summary>
        /// <remarks>可配置【di-pattern】缩小依赖注入程序集范围，作为<see cref="System.IO.Directory.GetFiles(string, string)"/>的第二个参数，默认:“*”。</remarks>
        /// <remarks>可配置【di-singleton】决定注入控制器构造函数时，是否参数单例模式注入，默认：true。</remarks>
#endif
        public static IServiceCollection UseDependencyInjection(this IServiceCollection services)
        {
#if NET_CORE
            string pattern = "di:pattern".Config("*");
#else
            string pattern = "di-pattern".Config("*");
#endif

            var assemblys = AssemblyFinder.Find(pattern);

            var assemblyTypes = assemblys
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass || x.IsInterface)
                .ToList();

#if NET_CORE
            var controllerBaseType = typeof(ControllerBase);
#else
            var controllerBaseType = typeof(ApiController);
#endif

            var controllerTypes = assemblyTypes
                .Where(x => x.IsClass && !x.IsAbstract && controllerBaseType.IsAssignableFrom(x))
                .ToList();

#if NET_CORE
            int maxDepth = "di:depth".Config(5);
            bool isSingleton = "di:singleton".Config(false);
#else
            int maxDepth = "di-depth".Config(5);
            bool isSingleton = "di-singleton".Config(false);
#endif

            foreach (var controllerType in controllerTypes)
            {
                bool flag = false;

                foreach (var constructorInfo in controllerType.GetConstructors().Where(x => x.IsPublic))
                {
                    flag = true;

                    foreach (var parameterInfo in constructorInfo.GetParameters())
                    {
                        if (parameterInfo.IsOptional || Di(services, parameterInfo.ParameterType, assemblyTypes, 0, maxDepth, isSingleton))
                        {
                            continue;
                        }

                        flag = false;

                        break;
                    }

                    if (flag)
                    {
                        break;
                    }
                }

                if (flag)
                {
                    continue;
                }

                throw new TypeLoadException($"Controller '{controllerType.FullName}' cannot be created and the current maximum dependency injection depth is {maxDepth}.");
            }

            return services;
        }

        private static bool Di(IServiceCollection services, Type serviceType, List<Type> assemblyTypes, int depth, int maxDepth, bool isSingleton)
        {
            if (services.Any(x => x.ServiceType == serviceType))
            {
                return true;
            }

            if (depth >= maxDepth)
            {
                if (serviceType.IsGenericType)
                {
                    var typeDefinition = serviceType.GetGenericTypeDefinition();

                    if (services.Any(x => x.ServiceType == typeDefinition))
                    {
                        return true;
                    }
                }

                return false;
            }

            var implementationTypes = (serviceType.IsInterface || serviceType.IsAbstract)
                ? assemblyTypes
                    .Where(y => y.IsClass && !y.IsAbstract && serviceType.IsAssignableFrom(y))
                    .ToList()
                : new List<Type> { serviceType };

            bool flag = false;

            foreach (var implementationType in implementationTypes)
            {
                foreach (var constructorInfo in implementationType.GetConstructors().Where(x => x.IsPublic))
                {
                    flag = true;

                    foreach (var parameterInfo in constructorInfo.GetParameters())
                    {
                        if (parameterInfo.IsOptional)
                        {
                            continue;
                        }

                        if (Di(services, parameterInfo.ParameterType, assemblyTypes, depth + 1, maxDepth, isSingleton))
                        {
                            if (isSingleton)
                            {
                                services.AddSingleton(serviceType, implementationType);
                            }
                            else
                            {
                                services.AddTransient(serviceType, implementationType);
                            }

                            continue;
                        }

                        flag = false;

                        break;
                    }

                    if (flag)
                    {
                        break;
                    }
                }

                if (flag)
                {
                    if (isSingleton)
                    {
                        services.AddSingleton(serviceType, implementationType);
                    }
                    else
                    {
                        services.AddTransient(serviceType, implementationType);
                    }

                    break;
                }
            }

            if (!flag)
            {
                if (serviceType.IsGenericType)
                {
                    var typeDefinition = serviceType.GetGenericTypeDefinition();

                    if (services.Any(x => x.ServiceType == typeDefinition))
                    {
                        return true;
                    }
                }
            }

            return flag;
        }
    }
}