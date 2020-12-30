#if NET_CORE
using CodeArts.Db;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <param name="lifetime">声明周期。</param>
        /// <returns></returns>
        public static IServiceCollection AddDefaultRepositories<TContext>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped) where TContext : DbContext
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
                        var iRepositoryType2 = typeof(ILinqRepository<,>).MakeGenericType(entityType, keyType);

                        var repositoryType2 = typeof(LinqRepository<,>).MakeGenericType(entityType, keyType);

                        services.Add(new ServiceDescriptor(iRepositoryType2, derviceProvider =>
                        {
                            var context = derviceProvider.GetService(contextType);

                            if (context is null)
                            {
                                return null;
                            }

                            return Activator.CreateInstance(repositoryType2, new object[1] { context });

                        }, lifetime));
                    }

                    var repositoryType = typeof(LinqRepository<>).MakeGenericType(entityType);
                    var iRepositoryType = typeof(ILinqRepository<>).MakeGenericType(entityType);

                    services.Add(new ServiceDescriptor(iRepositoryType, derviceProvider =>
                    {
                        var context = derviceProvider.GetService(contextType);

                        if (context is null)
                        {
                            return null;
                        }

                        return Activator.CreateInstance(repositoryType, new object[1] { context });

                    }, lifetime));
                });

            return services.AddDbContext<TContext>();
        }
    }
}
#endif