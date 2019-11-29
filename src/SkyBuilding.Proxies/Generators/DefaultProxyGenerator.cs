using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SkyBuilding.Proxies.Generators
{
    /// <summary>
    /// 默认代理器。
    /// </summary>
    /// <remarks>不支持参数含【out、ref】等<see cref="Type.IsByRef"/>属性为真的方法代理!</remarks>
    public class DefaultProxyGenerator : IProxyGenerator
    {
        private static readonly Type interceptType = typeof(IIntercept);
        private static readonly Type interceptorType = typeof(IInterceptor);
        private static readonly MethodInfo interceptMethod = interceptorType.GetMethod("Intercept", new Type[] { interceptType });
        private static readonly MethodInfo interceptStaticMethod = typeof(DefaultProxyGenerator).GetMethod(nameof(Intercept), BindingFlags.Public | BindingFlags.Static);
        private static readonly ConstructorInfo interceptConstructor = typeof(Intercept).GetConstructor(new Type[] { typeof(object), typeof(MethodInfo), typeof(object[]) });
        private static readonly ConstructorInfo objectConstructor = typeof(object).GetConstructor(Type.EmptyTypes);
        private static readonly MethodInfo returnValueMethod = interceptType.GetMethod("get_ReturnValue", Type.EmptyTypes);
        private static readonly MethodInfo getMethodFromHandleMethod = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) });
        private static readonly ConstructorInfo invalidOperationExceptionConstructor = typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string) });

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DefaultProxyGenerator() : this(new ModuleScope())
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="scope">模块范围</param>
        public DefaultProxyGenerator(ModuleScope scope) => Scope = scope ?? throw new ArgumentNullException(nameof(scope));

        /// <summary>
        /// 模块范围。
        /// </summary>
        public ModuleScope Scope { get; }

#if NETSTANDARD2_1
        /// <summary>
        /// 仅用作内部使用（解决.NETCOREAPP3.0下，运行时“Bad Il format.”异常）。
        /// </summary>
        /// <param name="interceptor">拦截器</param>
        /// <param name="intercept">拦截信息</param>
        /// <returns>拦截方法的返回值<see cref="IIntercept.ReturnValue"/></returns>
        public static object Intercept(IInterceptor interceptor, IIntercept intercept)
        {
            interceptor.Intercept(intercept);

            return intercept.ReturnValue;
        }
#endif

        private static void ProxyInterfaceMethods(TypeBuilder typeBuilder, ILGenerator ilOfStaticCtor, FieldBuilder interceptorField, FieldBuilder instanceField, MethodInfo[] methods, ProxyOptions options)
        {
            foreach (MethodInfo method in methods)
            {
                var parameters = method.GetParameters();
                var parameterTypes = parameters.Select(x => x.ParameterType).ToArray();
                var methodBuilder = typeBuilder.DefineMethod(method.Name,
                    MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.Standard);

                if (method.IsGenericMethod)
                {
                    var genericArguments = method.GetGenericArguments();

                    var newGenericParameters = methodBuilder.DefineGenericParameters(genericArguments.Select(x => x.Name).ToArray());

                    foreach (var item in genericArguments.Zip(newGenericParameters, (g, t) =>
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

                if (!options.Hook.IsEnabledFor(method))
                {
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldfld, instanceField);
                    parameters.ForEach((info, index) =>
                    {
                        ilGen.Emit(OpCodes.Ldarg, index + 1);
                    });

                    ilGen.Emit(OpCodes.Callvirt, method);

                    goto return_label;
                }

                if (parameters.Any(x => x.ParameterType.IsByRef))
                    throw new NotSupportedException("不支持包含“out”、“ref”参数的方法代理!");

                LocalBuilder local = CreateIntercept(typeBuilder, ilOfStaticCtor, ilGen, instanceField, method.IsGenericMethod ? method.MakeGenericMethod(methodBuilder.GetGenericArguments()) : method, parameterTypes);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, interceptorField);
                ilGen.Emit(OpCodes.Ldloc, local);
#if NETSTANDARD2_1
                ilGen.Emit(OpCodes.Call, interceptStaticMethod);
#else
                ilGen.Emit(OpCodes.Call, interceptMethod);
#endif

                if (method.ReturnType == typeof(void))
                {
                    ilGen.Emit(OpCodes.Pop);

                    goto return_label;
                }

#if !NETSTANDARD2_1
                ilGen.Emit(OpCodes.Callvirt, returnValueMethod);
#endif

                if (method.ReturnType.IsValueType && !method.ReturnType.IsNullable())
                {
                    var value = ilGen.DeclareLocal(typeof(object));

                    ilGen.Emit(OpCodes.Stloc, value);

                    ilGen.Emit(OpCodes.Ldloc, value);

                    var nullLabel = ilGen.DefineLabel();

                    ilGen.Emit(OpCodes.Ldnull);

                    ilGen.Emit(OpCodes.Ceq);

                    ilGen.Emit(OpCodes.Brfalse_S, nullLabel);

                    ilGen.Emit(OpCodes.Nop);

                    ilGen.Emit(OpCodes.Ldstr, "拦截器未能设置返回值，或目标抛出的异常!");

                    ilGen.Emit(OpCodes.Newobj, invalidOperationExceptionConstructor);

                    ilGen.Emit(OpCodes.Throw);

                    ilGen.MarkLabel(nullLabel);

                    ilGen.Emit(OpCodes.Ldloc, value);
                }

                if (method.ReturnType.IsValueType || method.ReturnType.IsGenericParameter)
                {
                    ilGen.Emit(OpCodes.Unbox_Any, method.ReturnType);
                }
                else
                {
                    ilGen.Emit(OpCodes.Castclass, method.ReturnType);
                }

            return_label:
                {
                    ilGen.Emit(OpCodes.Ret);

#if NET40 || NETSTANDARD2_0 || NETSTANDARD2_1
                    typeBuilder.DefineMethodOverride(methodBuilder, method);
#endif
                }
            }
        }

        private static void ProxyClassMethods(TypeBuilder typeBuilder, ILGenerator ilOfStaticCtor, FieldBuilder interceptorField, MethodInfo[] methods, ProxyOptions options)
        {
            foreach (MethodInfo method in methods)
            {
                if (!method.IsPublic || method.IsStatic || !options.Hook.IsEnabledFor(method)) continue;

                var parameters = method.GetParameters();
                var parameterTypes = parameters.Select(x => x.ParameterType).ToArray();
                var methodBuilder = typeBuilder.DefineMethod(method.Name,
                    MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.Final,
                    CallingConventions.Standard);

                if (method.IsGenericMethod)
                {
                    var genericArguments = method.GetGenericArguments();

                    var newGenericParameters = methodBuilder.DefineGenericParameters(genericArguments.Select(x => x.Name).ToArray());

                    foreach (var item in genericArguments.Zip(newGenericParameters, (g, t) =>
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

                if (parameters.Any(x => x.ParameterType.IsByRef))
                    throw new NotSupportedException("不支持包含“out”、“ref”参数方法的代理!");

                LocalBuilder local = CreateIntercept(typeBuilder, ilOfStaticCtor, ilGen, method.IsGenericMethod ? method.MakeGenericMethod(methodBuilder.GetGenericArguments()) : method, parameterTypes);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, interceptorField);
                ilGen.Emit(OpCodes.Ldloc, local);
                ilGen.Emit(OpCodes.Call, interceptMethod);

                if (method.ReturnType == typeof(void))
                {
                    ilGen.Emit(OpCodes.Pop);

                    goto return_label;
                }

                ilGen.Emit(OpCodes.Callvirt, returnValueMethod);

                if (method.ReturnType.IsValueType && !(method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                {
                    var value = ilGen.DeclareLocal(typeof(object));

                    ilGen.Emit(OpCodes.Stloc, value);

                    ilGen.Emit(OpCodes.Ldloc, value);

                    var nullLabel = ilGen.DefineLabel();

                    ilGen.Emit(OpCodes.Ldnull);

                    ilGen.Emit(OpCodes.Ceq);

                    ilGen.Emit(OpCodes.Brfalse_S, nullLabel);

                    ilGen.Emit(OpCodes.Nop);

                    ilGen.Emit(OpCodes.Ldstr, "拦截器未能设置返回值，或目标抛出的异常!");

                    ilGen.Emit(OpCodes.Newobj, invalidOperationExceptionConstructor);

                    ilGen.Emit(OpCodes.Throw);

                    ilGen.MarkLabel(nullLabel);

                    ilGen.Emit(OpCodes.Ldloc, value);
                }

                if (method.ReturnType.IsValueType || method.ReturnType.IsGenericParameter)
                {
                    ilGen.Emit(OpCodes.Unbox_Any, method.ReturnType);
                }
                else
                {
                    ilGen.Emit(OpCodes.Castclass, method.ReturnType);
                }

            return_label:

                ilGen.Emit(OpCodes.Ret);
            }
        }

        private static LocalBuilder CreateIntercept(TypeBuilder typeBuilder, ILGenerator ilOfStaticCtor, ILGenerator ilGen, FieldBuilder instanceField, MethodInfo method, Type[] parameters)
        {
            //? 上下文
            var local = ilGen.DeclareLocal(typeof(IIntercept));

            //! 声明一个类型为object的局部数组
            LocalBuilder array = ilGen.DeclareLocal(typeof(object[]));

            //? 数组长度入栈
            ilGen.Emit(OpCodes.Ldc_I4, parameters.Length);

            //! 生成一个object的数组。
            ilGen.Emit(OpCodes.Newarr, typeof(object));

            parameters.ForEach((type, index) =>
            {
                ilGen.Emit(OpCodes.Dup);
                //? 数组下标入栈
                ilGen.Emit(OpCodes.Ldc_I4, index);
                //? 加载对应下标的参数。（第0个是this。）
                ilGen.Emit(OpCodes.Ldarg, index + 1);

                //! 值类型参数装箱。
                if (type.IsValueType || type.IsGenericParameter)
                {
                    ilGen.Emit(OpCodes.Box, type);
                }

                //! 将参数存入数组。
                ilGen.Emit(OpCodes.Stelem_Ref);
            });

            //? 给数组赋值。
            ilGen.Emit(OpCodes.Stloc, array);

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, instanceField);

            if (method.IsGenericMethod)
            {
                ilGen.Emit(OpCodes.Ldtoken, method);

                ilGen.Emit(OpCodes.Ldtoken, method.DeclaringType);

                ilGen.Emit(OpCodes.Call, getMethodFromHandleMethod);

                ilGen.Emit(OpCodes.Castclass, typeof(MethodInfo));
            }
            else
            {
                var tokenField = typeBuilder.DefineField("token_" + method.Name + method.MetadataToken, typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                ilOfStaticCtor.Emit(OpCodes.Ldtoken, method);

                ilOfStaticCtor.Emit(OpCodes.Ldtoken, method.DeclaringType);

                ilOfStaticCtor.Emit(OpCodes.Call, getMethodFromHandleMethod);

                ilOfStaticCtor.Emit(OpCodes.Castclass, typeof(MethodInfo));

                ilOfStaticCtor.Emit(OpCodes.Stsfld, tokenField);

                ilGen.Emit(OpCodes.Ldsfld, tokenField);
            }

            ilGen.Emit(OpCodes.Ldloc, array);

            //? 生成对象。
            ilGen.Emit(OpCodes.Newobj, interceptConstructor);

            ilGen.Emit(OpCodes.Stloc, local);

#if !NETSTANDARD2_1
            ilGen.Emit(OpCodes.Ldloc, local);
#endif

            return local;
        }

        private static LocalBuilder CreateIntercept(TypeBuilder typeBuilder, ILGenerator ilOfStaticCtor, ILGenerator ilGen, MethodInfo method, Type[] parameters)
        {
            //! 声明一个类型为object的局部数组
            LocalBuilder array = ilGen.DeclareLocal(typeof(object[]));

            //? 上下文
            var local = ilGen.DeclareLocal(typeof(IIntercept));

            //? 数组长度入栈
            ilGen.Emit(OpCodes.Ldc_I4, parameters.Length);

            //! 生成一个object的数组。
            ilGen.Emit(OpCodes.Newarr, typeof(object));

            parameters.ForEach((type, index) =>
            {
                ilGen.Emit(OpCodes.Dup);
                //? 数组下标入栈
                ilGen.Emit(OpCodes.Ldc_I4, index);
                //? 加载对应下标的参数。（第0个是this。）
                ilGen.Emit(OpCodes.Ldarg, index + 1);

                //! 值类型参数装箱。
                if (type.IsValueType || type.IsGenericParameter)
                {
                    ilGen.Emit(OpCodes.Box, type);
                }

                //! 将参数存入数组。
                ilGen.Emit(OpCodes.Stelem_Ref);
            });

            //? 
            ilGen.Emit(OpCodes.Stloc, array);

            ilGen.Emit(OpCodes.Ldarg_0);

            if (method.IsGenericMethod)
            {
                ilGen.Emit(OpCodes.Ldtoken, method);

                ilGen.Emit(OpCodes.Ldtoken, method.DeclaringType);

                ilGen.Emit(OpCodes.Call, getMethodFromHandleMethod);

                ilGen.Emit(OpCodes.Castclass, typeof(MethodInfo));
            }
            else
            {
                var tokenField = typeBuilder.DefineField("token_" + method.Name + method.MetadataToken, typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                ilOfStaticCtor.Emit(OpCodes.Ldtoken, method);

                ilOfStaticCtor.Emit(OpCodes.Ldtoken, method.DeclaringType);

                ilOfStaticCtor.Emit(OpCodes.Call, getMethodFromHandleMethod);

                ilOfStaticCtor.Emit(OpCodes.Castclass, typeof(MethodInfo));

                ilOfStaticCtor.Emit(OpCodes.Stsfld, tokenField);

                ilGen.Emit(OpCodes.Ldsfld, tokenField);
            }

            ilGen.Emit(OpCodes.Ldloc, array);

            //? 生成对象。
            ilGen.Emit(OpCodes.Newobj, interceptConstructor);

            ilGen.Emit(OpCodes.Stloc, local);
            ilGen.Emit(OpCodes.Ldloc, local);

            return local;
        }

        /// <summary>
        /// 获取指定类型的代理类。
        /// </summary>
        /// <param name="interfaceType">指定类型</param>
        /// <param name="options">代理配置</param>
        /// <returns></returns>
        public TypeBuilder Of(Type interfaceType, ProxyOptions options)
        {
            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException("指定类型不是接口!", nameof(interfaceType));
            }

            var moduleBuilder = Scope.Create(interfaceType);

            var typeBuilder = moduleBuilder.DefineType(interfaceType.Name + "Proxy" + options.MetadataToken, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, typeof(object), new Type[] { interfaceType });

            var instanceField = typeBuilder.DefineField("instance", interfaceType, FieldAttributes.Private | FieldAttributes.InitOnly);

            var interceptorField = typeBuilder.DefineField("interceptor", interceptorType, FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { interfaceType, interceptorType });

            constructorBuilder.DefineParameter(1, ParameterAttributes.None, "instance");
            constructorBuilder.DefineParameter(2, ParameterAttributes.None, "interceptor");

            var ilOfCtor = constructorBuilder.GetILGenerator();

            ilOfCtor.Emit(OpCodes.Ldarg_0);
            ilOfCtor.Emit(OpCodes.Call, objectConstructor);
            ilOfCtor.Emit(OpCodes.Ldarg_0);
            ilOfCtor.Emit(OpCodes.Ldarg_1);
            ilOfCtor.Emit(OpCodes.Stfld, instanceField);
            ilOfCtor.Emit(OpCodes.Ldarg_0);
            ilOfCtor.Emit(OpCodes.Ldarg_2);
            ilOfCtor.Emit(OpCodes.Stfld, interceptorField);

            ilOfCtor.Emit(OpCodes.Ret);

            var constructorStaticBuilder = typeBuilder.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);

            var ilOfStaticCtor = constructorStaticBuilder.GetILGenerator();

            Array.ForEach(ProxyGenerator.GetAllInterfaces(interfaceType), type => ProxyInterfaceMethods(typeBuilder, ilOfStaticCtor, interceptorField, instanceField, type.GetMethods(), options));

            ilOfStaticCtor.Emit(OpCodes.Ret);

            return typeBuilder;
        }

        /// <summary>
        /// 获取指定类型的代理类。(仅代理类实现的方法，隐式转为接口时，不会拦截!)
        /// </summary>
        /// <param name="classType">代理类</param>
        /// <param name="options">代理配置</param>
        /// <returns></returns>
        public TypeBuilder New(Type classType, ProxyOptions options)
        {
            if (!classType.IsClass || classType.IsAbstract)
            {
                throw new ArgumentException("指定类型不是可实例化的类!", nameof(classType));
            }

            if (classType.IsNested)
            {
                throw new ArgumentException("不支持密封类的代理!", nameof(classType));
            }

            var moduleBuilder = Scope.Create(classType);

            var typeBuilder = moduleBuilder.DefineType(classType.Name + "Proxy" + options.MetadataToken, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, classType);

            var interceptorField = typeBuilder.DefineField("interceptor", interceptorType, FieldAttributes.Private | FieldAttributes.InitOnly);

            foreach (var item in classType.GetConstructors())
            {
                if (!item.IsPublic || item.IsStatic) continue;

                var parameters = item.GetParameters();

                var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, (new Type[] { interceptorType }).Concat(parameters.Select(x => x.ParameterType)).ToArray());

                constructorBuilder.DefineParameter(1, ParameterAttributes.None, "interceptor");

                parameters.ForEach((info, index) =>
                {
                    constructorBuilder.DefineParameter(index + 2, info.Attributes, info.Name);
                });

                var ilOfCtor = constructorBuilder.GetILGenerator();

                ilOfCtor.Emit(OpCodes.Ldarg_0);
                parameters.ForEach((info, index) =>
                {
                    ilOfCtor.Emit(OpCodes.Ldarg, index + 2);
                });
                ilOfCtor.Emit(OpCodes.Call, item);
                ilOfCtor.Emit(OpCodes.Ldarg_0);
                ilOfCtor.Emit(OpCodes.Ldarg_1);
                ilOfCtor.Emit(OpCodes.Stfld, interceptorField);

                ilOfCtor.Emit(OpCodes.Ret);
            }

            var constructorStaticBuilder = typeBuilder.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);

            var ilOfStaticCtor = constructorStaticBuilder.GetILGenerator();

            ProxyClassMethods(typeBuilder, ilOfStaticCtor, interceptorField, classType.GetMethods(), options);

            ilOfStaticCtor.Emit(OpCodes.Ret);

            return typeBuilder;
        }
    }
}
