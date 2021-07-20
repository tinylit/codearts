#if NETSTANDARD2_0_OR_GREATER
using CodeArts.Db;
using CodeArts.Db.EntityFramework;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// EF 注入。
    /// </summary>
    public static class EntityFrameworkServiceCollectionExtentions
    {
        private static readonly Type DbContextType = typeof(DbContext);

        /// <summary>
        /// 注册上下文（并注册上下文中数据表的仓库支持）。
        /// </summary>
        /// <typeparam name="TContext">数据库上下文。</typeparam>
        /// <param name="services">服务集合。</param>
        /// <param name="lifetime">在容器中注册仓库和DbContext服务的生存期。</param>
        /// <param name="optionsLifetime">在容器中注册DbContextOptions服务的生存期。</param>
        /// <returns></returns>
        public static IServiceCollection AddDefaultRepositories<TContext>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped, ServiceLifetime optionsLifetime = ServiceLifetime.Scoped) where TContext : DbContext
        {
            var contextType = typeof(TContext);

            var propertys = contextType.GetProperties();

            propertys
                .Where(x => x.PropertyType.IsGenericType && x.PropertyType.DeclaringType != DbContextType && x.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .ForEach(x =>
                {
                    var entityType = x.PropertyType.GetGenericArguments()[0];

                    if (!typeof(IEntiy).IsAssignableFrom(entityType))
                    {
                        return;
                    }

                    bool flag = false;
                    Type keyType = null;
                    var targetType = entityType;

                    do
                    {
                        var interfaces = targetType.GetInterfaces();

                        if (interfaces.Length > 0)
                        {
                            foreach (var interfaceType in interfaces)
                            {
                                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEntiy<>))
                                {
                                    keyType = interfaceType.GetGenericArguments()[0];

                                    flag = true;

                                    break;
                                }
                            }
                        }

                        targetType = targetType.BaseType;

                        if (flag || targetType is null || targetType == typeof(object))
                        {
                            break;
                        }

                    } while (true);

                    if (flag)
                    {
                        var repositoryType2 = typeof(LinqRepository<,>).MakeGenericType(entityType, keyType);

                        services.Add(new ServiceDescriptor(typeof(ILinqRepository<,>).MakeGenericType(entityType, keyType), CreateNewInstance(repositoryType2, contextType), lifetime));
                    }

                    var repositoryType = typeof(LinqRepository<>).MakeGenericType(entityType);

                    services.Add(new ServiceDescriptor(typeof(ILinqRepository<>).MakeGenericType(entityType), CreateNewInstance(repositoryType, contextType), lifetime));
                });

            return services.AddDbContext<TContext>(lifetime, optionsLifetime);
        }

        private readonly static MethodInfo GetServiceMtd = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService));

        private static Func<IServiceProvider, object> CreateNewInstance(Type repositoryType, Type contextType)
        {
            foreach (var item in repositoryType.GetConstructors())
            {
                var parameters = item.GetParameters();

                if (parameters.Length != 1)
                {
                    continue;
                }

                if (!parameters.All(x => contextType == x.ParameterType || x.ParameterType == DbContextType))
                {
                    continue;
                }

                var serviceProExp = Expression.Parameter(typeof(IServiceProvider));

                var callExp = Expression.Call(serviceProExp, GetServiceMtd, Expression.Constant(contextType));

                var newExp = Expression.New(item, new Expression[1] { Expression.Convert(callExp, contextType) });

                var lambdaEx = Expression.Lambda<Func<IServiceProvider, object>>(newExp, serviceProExp);

                return lambdaEx.Compile();
            }

            throw new NotSupportedException($"默认仓库未声明{repositoryType.Name}({contextType.Name} context)构造函数！");
        }
    }
}
#endif