using CodeArts.DbAnnotations;
using CodeArts.Emit;
using CodeArts.Emit.Expressions;
using CodeArts.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static CodeArts.Emit.AstExpression;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据工厂。
    /// </summary>
    public class DbTypeGen : ITypeGen
    {
        private static readonly Type[] supportGenericTypes = new Type[] { typeof(IDbMapper<>), typeof(IDbRepository<>), typeof(IRepository<>), typeof(IOrderedQueryable<>), typeof(IQueryable<>), typeof(IEditable<>), typeof(IEnumerable<>) };
        private static readonly Type[] supportTypes = new Type[] { typeof(IQueryable), typeof(IOrderedQueryable), typeof(IQueryProvider), typeof(IEditable), typeof(ISelectable), typeof(IEnumerable) };
        private static readonly ConcurrentDictionary<Type, Type> TypeCache = new ConcurrentDictionary<Type, Type>();
        private static readonly MethodInfo DictionaryAdd = typeof(Dictionary<string, object>).GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo MapToMethod = typeof(ObjectExtentions).GetMethod(nameof(ObjectExtentions.MapTo), new Type[] { typeof(object), typeof(Type) });
        private static readonly MethodInfo QueryFirstOrDefaultMethod = typeof(ISelectable).GetMethod(nameof(ISelectable.QueryFirstOrDefault));
        private static readonly MethodInfo QueryFirstMethod = typeof(ISelectable).GetMethod(nameof(ISelectable.QueryFirst));
        private static readonly MethodInfo QueryMethod = typeof(ISelectable).GetMethod(nameof(ISelectable.Query));

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
                var interfaces = interfaceType.GetInterfaces();

                var mapperType = interfaces.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDbMapper<>)) ?? throw new NotSupportedException($"接口“{interfaceType.FullName}”未继承“IDbMapper<>”接口!");

                if (!interfaces.All(x => x.IsGenericType ? supportGenericTypes.Contains(x.GetGenericTypeDefinition()) : supportTypes.Contains(x)))
                {
                    throw new NotSupportedException("支持继承的接口：typeof(IDbMapper<>), typeof(IDbRepository<>), typeof(IRepository<>), typeof(IOrderedQueryable<>), typeof(IQueryable<>), typeof(IEditable<>), typeof(IEnumerable<>), typeof(IQueryable), typeof(IOrderedQueryable), typeof(IQueryProvider), typeof(IEditable), typeof(ISelectable), typeof(IEnumerable)。");
                }

                var typeArguments = mapperType.GetGenericArguments();

                var typeArgument = typeArguments.First();

                foreach (var typeItem in interfaces)
                {
                    InterfaceCheck(typeItem, typeArgument);
                }

                var typeStore = RuntimeTypeCache.Instance.GetCache(type);

                foreach (var item in typeStore.MethodStores)
                {
                    MethodCheck(item);
                }

                return Create(typeStore, new Type[] { interfaceType }, typeArgument);
            });
        }

        private static void InterfaceCheck(Type interfaceType, Type typeArgument)
        {
            if (interfaceType.IsGenericType)
            {
                var typeArguments = interfaceType.GetGenericArguments();

                if (typeArguments.Length > 1)
                {
                    throw new NotSupportedException($"“{interfaceType}”具有一个以上的泛型参数!");
                }

                var typeArgument2 = typeArguments.First();

                if (typeArgument != typeArgument2)
                {
                    throw new NotSupportedException($"“{interfaceType}”的泛型约束“{typeArgument2}”和映射接口的泛型约束“{typeArgument}”不一致!");
                }
            }
        }

        private static void MethodCheck(MethodStoreItem storeItem)
        {
            if (!storeItem.IsDefined<SqlAttribute>())
            {
                throw new NotSupportedException($"函数“{storeItem.Name}”未标记操作指令!");
            }

            foreach (var item in storeItem.ParameterStores)
            {
                if (item.Info.ParameterType.IsByRef)
                {
                    throw new NotSupportedException($"函数“{storeItem.Name}”中，名称为“{item.Name}”的参数包含“out”或“ref”!");
                }
            }
        }

        private Type Create(TypeStoreItem storeItem, Type[] interfaces, Type typeArgument)
        {
            Type repositoryType;

            var repositoryAttr = storeItem.GetCustomAttribute<RepositoryAttribute>();

            if (!storeItem.MethodStores.All(x => x.IsDefined<SelectAttribute>()) || interfaces.Any(x =>
              {
                  if (x.IsGenericType)
                  {
                      var typeDefinition = x.GetGenericTypeDefinition();

                      return typeDefinition == typeof(IDbRepository<>) || typeDefinition == typeof(IEditable<>);
                  }

                  return x == typeof(IEditable);
              }))
            {
                repositoryType = typeof(DbRepository<>).MakeGenericType(typeArgument);
            }
            else
            {
                repositoryType = typeof(Repository<>).MakeGenericType(typeArgument);
            }

            if (repositoryAttr is null)
            {
                return Create(storeItem, repositoryType, typeArgument, interfaces);
            }

            var repositoryAttrType = repositoryAttr.RepositoryType;

            if (repositoryAttrType.IsGenericTypeDefinition)
            {
                repositoryAttrType = repositoryAttrType.MakeGenericType(typeArgument);
            }

            if (repositoryType.IsAssignableFrom(repositoryAttrType))
            {
                return Create(storeItem, repositoryType, typeArgument, interfaces);
            }

            throw new NotSupportedException($"指定仓库（{repositoryAttr.RepositoryType.FullName}）需继承（{repositoryType.FullName}）仓库，才能支持当前接口的所有指令!");
        }

        private Type Create(TypeStoreItem storeItem, Type repositoryType, Type typeArgument, Type[] interfaces)
        {
            var moduleEmitter = new ModuleEmitter("CodeArts.ORM.DependencyInjection");

            var classEmitter = new ClassEmitter(moduleEmitter, storeItem.Naming + "Respository", TypeAttributes.Public, repositoryType, interfaces);

            foreach (var item in storeItem.Type.GetCustomAttributesData())
            {
#if NET40
                if (item.Constructor.DeclaringType == typeof(TypeGenAttribute))
#else
                if (item.AttributeType == typeof(TypeGenAttribute))
#endif
                {
                    continue;
                }

                classEmitter.DefineCustomAttribute(item);
            }

            Array.ForEach(repositoryType.GetConstructors(BindingFlags.Public | BindingFlags.Instance), x =>
            {
                var ctor = classEmitter.DefineConstructor(MethodAttributes.Public);

                Array.ForEach(x.GetParameters(), y =>
                {
                    var paramter = ctor.DefineParameter(y.ParameterType, y.Attributes, y.Name);

#if NET40
                    if (y.IsOptional)
#else
                    if (y.HasDefaultValue)
#endif
                    {
                        paramter.SetConstant(y.DefaultValue);
                    }
                });
            });


            var typeArgumentItem = MapperRegions.Resolve(typeArgument);

            storeItem.MethodStores.ForEach(x =>
            {
                var method = classEmitter.DefineMethod(x.Name, x.Member.Attributes & ~MethodAttributes.Abstract, x.MemberType);

                var variable_params = method.DeclareVariable(typeof(Dictionary<string, object>));

                method.Append(Assign(variable_params, New(typeof(Dictionary<string, object>))));

                classEmitter.DefineMethodOverride(method, x.Member);

                x.ParameterStores.ForEach(y =>
                {
                    var paramter = method.DefineParameter(y.ParameterType, y.Info.Attributes, y.Name);

#if NET40
                    if (y.IsOptional)
#else
                    if (y.HasDefaultValue)
#endif
                    {
                        paramter.SetConstant(y.DefaultValue);
                    }

                    method.Append(Call(DictionaryAdd, variable_params, Constant(y.Naming), Convert(paramter, typeof(object))));
                });

                bool required = false;
                string missingMsg = null;
                var sqlAttribute = x.GetCustomAttribute<SqlAttribute>() ?? throw new NotSupportedException($"方法“{x.Name}”未指定操作指令!");
                var timeOutAttr = x.GetCustomAttribute<TimeOutAttribute>();

                var sql = New(typeof(SQL), Constant(sqlAttribute.Sql));
                var timeOut = Constant(timeOutAttr?.Value, typeof(int?));

                switch (sqlAttribute)
                {
                    case SelectAttribute selectAttribute:
                        required = selectAttribute.Required;
                        missingMsg = selectAttribute.MissingMsg;
                        goto default;
                    default:

                        ConvertAst thisArg;

                        if (sqlAttribute.CommandType == CommandTypes.Select)
                        {
                            thisArg = Convert(This(repositoryType), typeof(ISelectable));
                        }
                        else if (sqlAttribute.CommandType == CommandTypes.Insert || sqlAttribute.CommandType == CommandTypes.Update || sqlAttribute.CommandType == CommandTypes.Delete)
                        {
                            thisArg = Convert(This(repositoryType), typeof(IEditable));
                        }
                        else
                        {
                            throw new NotSupportedException($"“{sqlAttribute.CommandType}”指令不被支持!");
                        }

                        if (sqlAttribute.CommandType == CommandTypes.Select)
                        {
                            if (x.MemberType.IsGenericType)
                            {
                                var typeArguments = x.MemberType.GetGenericArguments();

                                if (typeArguments.Length == 1 && !typeArguments.Any(y => y.IsKeyValuePair()))
                                {
                                    var typeDefinition = x.MemberType.GetGenericTypeDefinition();

                                    var valueType = typeof(IEnumerable<>).MakeGenericType(typeArguments);


                                    var queryMethod = QueryMethod.MakeGenericMethod(typeArguments);

                                    var bodyQuery = Call(queryMethod, thisArg, sql, variable_params, timeOut);

                                    if (valueType == x.MemberType)
                                    {
                                        method.Append(Return(bodyQuery));

                                        break;
                                    }

                                    var variable = method.DeclareVariable(valueType);

                                    method.Append(Assign(variable, bodyQuery));

                                    var valueCast = Call(MapToMethod, Convert(variable, typeof(object)), Constant(x.MemberType));

                                    method.Append(Return(Convert(valueCast, x.MemberType)));

                                    break;
                                }
                            }

                            if (required)
                            {
                                var queryFirstMethod = QueryFirstMethod.MakeGenericMethod(x.MemberType);

                                var bodyQueryFirst = Call(queryFirstMethod, thisArg, sql, Convert(variable_params, typeof(object)), Default(x.MemberType), Constant(timeOutAttr?.Value, typeof(int?)), Constant(missingMsg, typeof(string)));

                                method.Append(Return(Convert(bodyQueryFirst, x.MemberType)));

                                break;
                            }

                            var queryFirstOrDefaultMethod = QueryFirstOrDefaultMethod.MakeGenericMethod(x.MemberType);

                            var bodyQueryFirstOrDefault = Call(queryFirstOrDefaultMethod, thisArg, sql, Convert(variable_params, typeof(object)), Constant(timeOutAttr?.Value, typeof(int?)));

                            method.Append(Return(Convert(bodyQueryFirstOrDefault, x.MemberType)));

                            break;
                        }

                        string commandName;

                        if (sqlAttribute.CommandType == CommandTypes.Insert)
                        {
                            commandName = nameof(IEditable.Insert);
                        }
                        else if (sqlAttribute.CommandType == CommandTypes.Update)
                        {
                            commandName = nameof(IEditable.Update);
                        }
                        else if (sqlAttribute.CommandType == CommandTypes.Delete)
                        {
                            commandName = nameof(IEditable.Delete);
                        }
                        else
                        {
                            throw new NotSupportedException($"“{sqlAttribute.CommandType}”指令不被支持!");
                        }

                        var commandMethod = typeof(IEditable).GetMethod(commandName, new Type[] { typeof(SQL), typeof(object), typeof(int?) });

                        var bodyExcute = Call(commandMethod, thisArg, sql, Convert(variable_params, typeof(object)), timeOut);

                        if (x.MemberType == typeof(int))
                        {
                            method.Append(Return(bodyExcute));
                        }
                        else if (x.MemberType == typeof(bool))
                        {
                            method.Append(Return(GreaterThan(bodyExcute, Constant(0))));
                        }
                        else
                        {
                            throw new NotSupportedException($"标记操作指令“{sqlAttribute.CommandType}”的方法，仅支持返回Int32或Boolean类型数据。");
                        }
                        break;
                }
            });

            return classEmitter.CreateType();
        }
    }
}
