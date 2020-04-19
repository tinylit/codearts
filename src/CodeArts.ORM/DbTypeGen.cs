using CodeArts.DbAnnotations;
using CodeArts.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据工厂。
    /// </summary>
    public class DbTypeGen : ITypeGen
    {
        private static readonly Type[] interfaceTypes = typeof(IDbRepository<>).GetInterfaces();
        private static readonly Type selectableType = typeof(ISelectable);
        private static readonly MethodInfo QueryMethod = selectableType.GetMethod(nameof(ISelectable.Query));
        private static readonly MethodInfo QueryFirstMethod = selectableType.GetMethod(nameof(ISelectable.QueryFirst));
        private static readonly Type editableType = typeof(IEditable);
        private static readonly ConcurrentDictionary<Type, Type> TypeCache = new ConcurrentDictionary<Type, Type>();
        private static readonly ConcurrentDictionary<Assembly, ModuleBuilder> ModuleCache = new ConcurrentDictionary<Assembly, ModuleBuilder>();

        /// <summary>
        /// 创建模块。
        /// </summary>
        /// <returns></returns>
        public ModuleBuilder CreateModule(Type type) => ModuleCache.GetOrAdd(type.Assembly, assembly =>
        {
            var name = assembly.GetName().Name;

            var assemblyName = new AssemblyName(name + ".DbPxy");

#if NET40
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#elif NET45 || NET451 || NET452 || NET461
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#else
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#endif
            return assemblyBuilder.DefineDynamicModule(name + ".DbDynamic");
        });
        /// <summary>
        /// 创建类型。
        /// </summary>
        /// <param name="interfaceType">接口类型</param>
        /// <returns></returns>
        public Type Create(Type interfaceType)
        {
            if (!interfaceType.IsInterface)
            {
                throw new NotSupportedException($"“{interfaceType.FullName}”不是接口类型!");
            }

            return TypeCache.GetOrAdd(interfaceType, type =>
            {
                var typeStore = RuntimeTypeCache.Instance.GetCache(type);

                if (!typeStore.MethodStores.All(x => x.IsDefined<CommandAttribute>()))
                {
                    throw new NotSupportedException($"接口“{typeStore.Name}”中，并非所有方法都标记了操作指令!");
                }

                typeStore.MethodStores.ForEach(MethodCheck);

                return Create(typeStore);
            });
        }

        private void MethodCheck(MethodStoreItem storeItem)
        {
            var isCommandable = storeItem.IsDefined<CommandAbleAttribute>();

            foreach (var item in storeItem.ParameterStores)
            {
                if (item.Info.ParameterType.IsByRef)
                {
                    throw new NotSupportedException($"函数“{storeItem.Name}”中，名称为“{item.Name}”的参数包含“out”或“ref”!");
                }

                if (isCommandable && !item.IsDefined<ObjectiveAttribute>())
                {
                    throw new NotSupportedException($"函数“{storeItem.Name}”标记了执行力(CommandableAttribute)，但名称为“{item.Name}”的参数未指定参数目标!");
                }
            }
        }

        private Type Create(TypeStoreItem storeItem)
        {
            Type interfaceType = storeItem.Type;

            var interfaces = interfaceType.GetInterfaces();

            Type repositoryType = interfaces.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDbRepository<>)) ??
                interfaces.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRepository<>)) ?? throw new NotSupportedException($"“{interfaceType.FullName}”未继承仓库接口（IRepository<>或IDbRepository<>）!");

            var typeArgument = repositoryType.GetGenericArguments().First();

            var repositoryAttr = storeItem.GetCustomAttribute<RepositoryAttribute>();

            if (repositoryAttr is null)
            {
                if (repositoryType.GetGenericTypeDefinition() == typeof(IRepository<>) && storeItem.MethodStores.All(x => x.IsDefined<QueryableAttribute>() || x.IsDefined<QueryAttribute>()))
                {
                    return Create(storeItem, interfaceType, typeof(Repository<>).MakeGenericType(typeArgument), typeArgument);
                }

                return Create(storeItem, interfaceType, typeof(DbRepository<>).MakeGenericType(typeArgument), typeArgument);
            }

            var repositoryAttrType = repositoryAttr.RepositoryType;

            if (repositoryAttrType.IsGenericTypeDefinition)
            {
                repositoryAttrType = repositoryAttrType.MakeGenericType(typeArgument);
            }

            if (repositoryType.IsAssignableFrom(repositoryAttrType))
            {
                return Create(storeItem, interfaceType, repositoryAttrType, typeArgument);
            }

            throw new NotSupportedException($"指定仓库（{repositoryAttr.RepositoryType.FullName}）无法支撑接口（{storeItem.Name}）的所有命令!");
        }

        private Type Create(TypeStoreItem storeItem, Type interfaceType, Type repositoryType, Type typeArgument)
        {
            var moduleBuilder = CreateModule(storeItem.Type);

            var typeBuilder = moduleBuilder.DefineType(interfaceType.Name + "DbProxy" + interfaceType.MetadataToken, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, repositoryType, new Type[] { interfaceType });

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);

            foreach (var attributeData in CustomAttributeData.GetCustomAttributes(interfaceType))
            {
#if NET40
                if (attributeData.Constructor.ReflectedType == typeof(DbConfigAttribute))
#else
                if (attributeData.AttributeType == typeof(DbConfigAttribute))
#endif
                {
                    constructorBuilder.SetCustomAttribute(new CustomAttributeBuilder(attributeData.Constructor, attributeData.ConstructorArguments.Select(x => x.Value).ToArray()));
                }
            }

            var ilOfCtor = constructorBuilder.GetILGenerator();

            ilOfCtor.Emit(OpCodes.Ldarg_0);
            ilOfCtor.Emit(OpCodes.Call, repositoryType.GetConstructor(Type.EmptyTypes));

            ilOfCtor.Emit(OpCodes.Ret);

            var constructorStaticBuilder = typeBuilder.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);

            var ilOfStaticCtor = constructorStaticBuilder.GetILGenerator();

            ProxyInterfaceMethods(typeBuilder, ilOfStaticCtor, storeItem.MethodStores);

            ilOfStaticCtor.Emit(OpCodes.Ret);

            return typeBuilder;
        }

        private static void ProxyInterfaceMethods(TypeBuilder typeBuilder, ILGenerator ilOfStaticCtor, IEnumerable<MethodStoreItem> methods)
        {
            foreach (MethodStoreItem item in methods)
            {
                MethodInfo method = item.Member;

                var parameters = method.GetParameters();

                var parameterTypes = parameters.Select(x => x.ParameterType).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(method.Name,
                    MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.Standard);

                if (method.IsGenericMethod)
                {
                    var genericArguments = method.GetGenericArguments();

                    var newGenericParameters = methodBuilder.DefineGenericParameters(genericArguments.Select(x => x.Name).ToArray());

                    foreach (var _ in genericArguments.Zip(newGenericParameters, (g, t) =>
                    {
                        t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                        t.SetInterfaceConstraints(g.GetGenericParameterConstraints());

                        t.SetBaseTypeConstraint(g.BaseType);

                        return true;
                    })) { }
                }

                methodBuilder.SetReturnType(method.ReturnType);

                methodBuilder.SetParameters(parameterTypes);

                parameters.ForEach((p, index) =>
                {
                    methodBuilder.DefineParameter(index + 1, p.Attributes, p.Name);
                });

                var ilGen = methodBuilder.GetILGenerator();

                var commandAttribute = (CommandAttribute)Attribute.GetCustomAttribute(method, typeof(CommandAttribute)) ?? throw new NotSupportedException($"函数“{item.Name}”未设置操作指令!");
                var timeOutAttribute = (TimeOutAttribute)Attribute.GetCustomAttribute(method, typeof(TimeOutAttribute));

                switch (commandAttribute)
                {
                    case UpdateableAttribute updateableAttribute when item.ParameterStores.Count > 0 && item.ParameterStores.Any(x => x.IsDefined<UpdateSetAttribute>()):
                        ProxyCommandableMethods(typeBuilder, ilOfStaticCtor, item, updateableAttribute, timeOutAttribute);
                        break;
                    case UpdateableAttribute updateableAttribute:
                        throw new NotSupportedException($"函数“{item.Name}”设置了更新指令，但未指定更新字段!");
                    case CommandAbleAttribute commandableAttribute:
                        ProxyCommandableMethods(typeBuilder, ilOfStaticCtor, item, commandableAttribute, timeOutAttribute);
                        break;
                    case QueryAttribute queryAttribute when queryAttribute.Required:
                        ProxySqlRequiredMethods(typeBuilder, ilOfStaticCtor, item, queryAttribute, timeOutAttribute);
                        break;
                    case SqlAttribute sqlAttribute:
                        ProxySqlMethods(typeBuilder, ilOfStaticCtor, item, sqlAttribute, timeOutAttribute);
                        break;
                    default:
                        throw new NotSupportedException($"函数“{item.Name}”设置的指令不被支持!");
                }
            }
        }

        private static void ProxySqlMethods(TypeBuilder typeBuilder, ILGenerator ilOfStaticCtor, MethodStoreItem method, SqlAttribute sqlAttribute, TimeOutAttribute timeOutAttribute)
        {

        }

        private static void ProxySqlRequiredMethods(TypeBuilder typeBuilder, ILGenerator ilOfStaticCtor, MethodStoreItem method, QueryAttribute sqlAttribute, TimeOutAttribute timeOutAttribute)
        {

        }

        private static void ProxyCommandableMethods(TypeBuilder typeBuilder, ILGenerator ilOfStaticCtor, MethodStoreItem method, CommandAbleAttribute commandableAttribute, TimeOutAttribute timeOutAttribute)
        {

        }
    }
}
