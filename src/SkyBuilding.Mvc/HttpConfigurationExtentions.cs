#if NET45 || NET451 || NET452 || NET461
using Autofac;
using Autofac.Integration.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace SkyBuilding.Mvc
{
    /// <summary>
    /// 配置拓展
    /// </summary>
    public static class HttpConfigurationExtentions
    {
        /// <summary>
        /// 使用依赖注入（注入<see cref="ApiController"/>的构造函数参数类型，若引入了【SkyBuilding.ORM】，将会注入【<see cref="ApiController"/>的构造函数参数】以及【其参数类型的构造函数参数】中使用到的【数据仓库类型】）
        /// </summary>
        public static void UseDependencyResolver(this HttpConfiguration config) => config.UseDependencyResolver(IocRegisters);

        /// <summary>
        /// 使用依赖注入（注入<see cref="ApiController"/>的构造函数参数类型，若引入了【SkyBuilding.ORM】，将会注入【<see cref="ApiController"/>的构造函数参数】以及【其参数类型的构造函数参数】中使用到的【数据仓库类型】）
        /// </summary>
        public static void UseDependencyResolver(this HttpConfiguration config, Action<ContainerBuilder> configureContainer)
        {
            var builder = new ContainerBuilder();

            configureContainer.Invoke(builder);

            config.DependencyResolver = new AutofacWebApiDependencyResolver(builder.Build());
        }

        #region Private
        /// <summary>
        /// 依赖注入
        /// </summary>
        /// <param name="builder">依赖注入容器构造器</param>
        /// <returns></returns>
        public static void IocRegisters(this ContainerBuilder builder)
        {
            var assemblys = AssemblyFinder.FindAll();

            var assemblyTypes = assemblys
                .SelectMany(x => x.GetTypes().Where(y => y.IsClass || y.IsInterface))
                .ToList();

            var controllerTypes = assemblyTypes
                .Where(type => !type.IsAbstract && typeof(ApiController).IsAssignableFrom(type))
                .ToList();

            var parameterTypes = controllerTypes
                .SelectMany(type =>
                {
                    return type.GetConstructors()
                    .Where(x => x.IsPublic)
                    .SelectMany(x => x.GetParameters().Select(y => y.ParameterType));

                }).Distinct()
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

                builder.RegisterTypes(exactlyTypes.ToArray())
                    .AsSelf()
                    .AsImplementedInterfaces()
                    .SingleInstance();
            }

            builder.RegisterTypes(types.Union(parameterTypes).Union(controllerTypes).ToArray())
                .Where(type => type.IsInterface || type.IsClass)
                .AsSelf() //自身服务，用于没有接口的类
                .AsImplementedInterfaces(); //接口服务
        }
        #endregion
    }
}
#endif