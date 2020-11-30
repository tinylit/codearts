using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Collections.Generic
{
    /// <summary>
    /// 迭代扩展。
    /// </summary>
    public static class IEnumerableExtentions
    {
        /// <summary>
        /// 对数据中的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="action">要对数据源的每个元素执行的委托。</param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
            {
                action.Invoke(item);
            }
        }

        /// <summary>
        /// 对数据中的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="action">要对数据源的每个元素执行的委托。</param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            int index = -1;
            foreach (T item in source)
            {
                action.Invoke(item, index += 1);
            }
        }
    }
}

namespace ConsoleEmit
{
    /// <summary>
    /// 拦截器。
    /// </summary>
    public interface IInterceptor
    {
        /// <summary>
        /// 拦截。
        /// </summary>
        void Intercept(IIntercept intercept);
    }

    /// <summary>
    /// 调用者数据信息。
    /// </summary>
    public interface IIntercept
    {
        /// <summary>
        /// 调用参数。
        /// </summary>
        object[] Arguments { get; }

        /// <summary>
        /// 调用函数。
        /// </summary>
        MethodInfo Method { get; }

        /// <summary>
        /// 拦截的对象。
        /// </summary>
        object Instance { get; }

        /// <summary>
        /// 返回值。
        /// </summary>
        object ReturnValue { get; set; }

        /// <summary>
        /// 执行方法。
        /// </summary>
        void Proceed();

        /// <summary>
        /// 获取参数值。
        /// </summary>
        /// <param name="index">索引。</param>
        /// <returns></returns>
        object GetArgumentValue(int index);

        /// <summary>
        /// 重设参数值。
        /// </summary>
        /// <param name="index">索引。</param>
        /// <param name="value">参数值。</param>
        void SetArgumentValue(int index, object value);
    }
    public interface IEmit
    {
        void Test();

        bool TestValueType(int i);

        int TestInt32(int i);

        T TestG<T>(T i);

        T TestGConstraint<T>(T i) where T : struct;

        IEmit TestClas();
    }

    public class Emit : IEmit
    {
        public void Test()
        {

        }

        public bool TestValueType(int i) => i > 0;

        public int TestInt32(int i) => i;

        public IEmit TestClas() => this;

        public T TestG<T>(T i) => i;

        public T TestGConstraint<T>(T i) where T : struct => i;

        public IEmit TestClas(int i, out int b)
        {
            b = i;
            return this;
        }

        public object ReturnValue { get; set; }
    }

    public class Interceptor : IInterceptor
    {
        public void Intercept(IIntercept invokeBinder)
        {
            invokeBinder.Proceed();
        }
    }

    public class InvokeBinder : IIntercept
    {
        public InvokeBinder(object instance, MethodInfo method, object[] arguments)
        {
            Instance = instance;
            Method = method;
            Arguments = arguments;
        }

        public object[] Arguments { get; }

        public MethodInfo Method { get; }

        public object Instance { get; }

        public object ReturnValue
        {
            get;
            set;
        }

        public object GetArgumentValue(int index) => Arguments[index < 0 ? Arguments.Length + index : index];

        public void Proceed() => ReturnValue = Method.Invoke(Instance, Arguments);

        public void SetArgumentValue(int index, object value) => Arguments[index < 0 ? Arguments.Length + index : index] = value;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var assemblyName = new AssemblyName("CodeArts.Emit");

            var assemblyBuilder = AppDomain.CurrentDomain
                .DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("CodeArts.Module", "CodeArts.Emit.dll");

            var iEmitType = typeof(IEmit);

            var interceptorType = typeof(IInterceptor);

            var typeBuilder = moduleBuilder.DefineType("EmitProxy", TypeAttributes.Class | TypeAttributes.NotPublic, null, new Type[] { iEmitType });

            var instanceField = typeBuilder.DefineField("instance", iEmitType, FieldAttributes.Private | FieldAttributes.InitOnly);

            var interceptorField = typeBuilder.DefineField("interceptor", interceptorType, FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { iEmitType, interceptorType });

            constructorBuilder.DefineParameter(1, ParameterAttributes.None, "instance");
            constructorBuilder.DefineParameter(2, ParameterAttributes.None, "interceptor");

            var ilOfCtor = constructorBuilder.GetILGenerator();

            var objectType = typeof(object);

            ilOfCtor.Emit(OpCodes.Ldarg_0);
            ilOfCtor.Emit(OpCodes.Call, objectType.GetConstructor(new Type[0]));
            ilOfCtor.Emit(OpCodes.Ldarg_0);
            ilOfCtor.Emit(OpCodes.Ldarg_1);
            ilOfCtor.Emit(OpCodes.Stfld, instanceField);
            ilOfCtor.Emit(OpCodes.Ldarg_0);
            ilOfCtor.Emit(OpCodes.Ldarg_2);
            ilOfCtor.Emit(OpCodes.Stfld, interceptorField);

            ilOfCtor.Emit(OpCodes.Ret);

            var constructorStaticBuilder = typeBuilder.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);

            var ilOfStaticCtor = constructorStaticBuilder.GetILGenerator();

            var invokeType = typeof(InvokeBinder);
            var returnValue = typeof(IIntercept).GetProperty("ReturnValue");
            var invokeCtor = invokeType.GetConstructor(new Type[] { typeof(object), typeof(MethodInfo), typeof(object[]) });

            var interceptMethod = interceptorType.GetMethod("Intercept", new Type[] { typeof(IIntercept) });

            foreach (var method in iEmitType.GetMethods())
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

                LocalBuilder local = method.IsGenericMethod
                    ? CreateInvokeBinder(typeBuilder, ilOfStaticCtor, ilGen, instanceField, invokeCtor, method.MakeGenericMethod(methodBuilder.GetGenericArguments()), parameterTypes)
                    : CreateInvokeBinder(typeBuilder, ilOfStaticCtor, ilGen, instanceField, invokeCtor, method, parameterTypes);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, interceptorField);
                ilGen.Emit(OpCodes.Ldloc, local);
                ilGen.Emit(OpCodes.Call, interceptMethod);

                if (method.ReturnType == typeof(void))
                {
                    ilGen.Emit(OpCodes.Pop);
                }
                else
                {
                    ilGen.Emit(OpCodes.Callvirt, returnValue.GetMethod);

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

                        ilGen.Emit(OpCodes.Ldstr, "Interceptors failed to set a return value, or swallowed the exception thrown by the target.");

                        ilGen.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string) }));

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
                }

                ilGen.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(methodBuilder, method);
            }

            ilOfStaticCtor.Emit(OpCodes.Ret);

            var type = typeBuilder.CreateType();

            assemblyBuilder.Save("CodeArts.Emit.dll");

            IEmit emit = (IEmit)Activator.CreateInstance(type, new Emit(), new Interceptor());

            emit.Test();

            var g1 = emit.TestG(new object());

            var g2 = emit.TestG(18);

            var g3 = emit.TestGConstraint(DateTimeKind.Utc);

            var i = emit.TestInt32(8);

            var v = emit.TestValueType(1);

            var c = emit.TestClas();
        }

        private static LocalBuilder CreateInvokeBinder(TypeBuilder typeBuilder, ILGenerator ilOfStaticCtor, ILGenerator ilGen, FieldBuilder instanceField, ConstructorInfo invokeCtor, MethodInfo method, Type[] parameters)
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
            ilGen.Emit(OpCodes.Ldfld, instanceField);

            if (method.IsGenericMethod)
            {
                ilGen.Emit(OpCodes.Ldtoken, method);

                ilGen.Emit(OpCodes.Ldtoken, method.DeclaringType);

                ilGen.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));

                ilGen.Emit(OpCodes.Castclass, typeof(MethodInfo));
            }
            else
            {
                var token = string.Empty;

                foreach (var item in method.GetParameters())
                {
                    token += "_" + item.MetadataToken.GetHashCode();
                }

                var tokenField = typeBuilder.DefineField("token_" + method.Name + token, typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                ilOfStaticCtor.Emit(OpCodes.Ldtoken, method);

                ilOfStaticCtor.Emit(OpCodes.Ldtoken, method.DeclaringType);

                ilOfStaticCtor.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));

                ilOfStaticCtor.Emit(OpCodes.Castclass, typeof(MethodInfo));

                ilOfStaticCtor.Emit(OpCodes.Stsfld, tokenField);

                ilGen.Emit(OpCodes.Ldsfld, tokenField);
            }

            ilGen.Emit(OpCodes.Ldloc, array);

            //? 生成对象。
            ilGen.Emit(OpCodes.Newobj, invokeCtor);

            ilGen.Emit(OpCodes.Stloc, local);
            ilGen.Emit(OpCodes.Ldloc, local);

            return local;
        }
    }

    public class Test
    {
        public Test()
        {

        }
        public Test(object value)
        {

        }
    }
}
