using SkyBuilding.AOP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ConsoleEmit
{
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
    }

    public class BaseProxy
    {
        public void Intercept(IInvokeBinder interceptor)
        {

        }

        public object GetReturnValue()
        {
            return 0;
        }

        public static MethodInfo MakeGenericMethod(MethodInfo method, Type[] types)
        {
            return method;//.MakeGenericMethod(types);
        }
    }

    public class Interceptor : IInterceptor
    {
        public void Intercept(IInvokeBinder invokeBinder)
        {
            invokeBinder.Invoke();
        }
    }

    public class InvokeBinder : IInvokeBinder
    {
        private readonly ParameterInfo[] parameters;
        public InvokeBinder(object instance, MethodInfo method, object[] arguments)
        {
            Instance = instance;
            Method = method;
            Arguments = arguments;

            parameters = method.GetParameters();
        }

        public object[] Arguments { get; }

        public MethodInfo Method { get; }

        public object Instance { get; }

        private object returnValue = null;

        public object ReturnValue
        {
            get => returnValue;
            set
            {
                if (Valid(value, Method.ReturnType))
                {
                    returnValue = value;
                }
            }
        }

        public object GetArgumentValue(int index) => Arguments[index < 0 ? Arguments.Length + index : index];

        public void Invoke() => returnValue = Method.Invoke(Instance, Arguments);

        public void SetArgumentValue(int index, object value) => Arguments[index < 0 ? Arguments.Length + index : index] = Valid(value, parameters[index < 0 ? Arguments.Length + index : index].ParameterType) ? value : throw new ArgumentException("参数类型不匹配!");

        private static bool Valid(object value, Type conversionType)
        {
            if (value is null) return true;

            var sourceType = value.GetType();

            return sourceType == conversionType || conversionType.IsAssignableFrom(sourceType);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var assemblyName = new AssemblyName("SkyBuilding.Emit");

            var assemblyBuilder = AppDomain.CurrentDomain
                .DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("SkyBuilding.Module", @"SkyBuilding.Emit.dll");

            var iEmitType = typeof(IEmit);

            var interfaceBuilder = moduleBuilder.DefineType("IEmitProxy", TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.NotPublic, null, new Type[] { iEmitType });

            var interfaceType = interfaceBuilder.CreateType();

            var interceptorType = typeof(IInterceptor);

            var typeBuilder = moduleBuilder.DefineType("EmitProxy", TypeAttributes.Class | TypeAttributes.NotPublic, typeof(BaseProxy), new Type[] { interfaceType });

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

            var invokeType = typeof(InvokeBinder);
            var returnValue = typeof(IInvokeBinder).GetProperty("ReturnValue");
            var invokeCtor = invokeType.GetConstructor(new Type[] { typeof(object), typeof(MethodInfo), typeof(object[]) });

            var interceptMethod = interceptorType.GetMethod("Intercept", new Type[] { typeof(IInvokeBinder) });

            foreach (var method in iEmitType.GetMethods())
            {
                var parameters = method.GetParameters();
                var parameterTypes = parameters.Select(x => x.ParameterType).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(method.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
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

                MethodInfo methodInfo = method;

                if (method.IsGenericMethod)
                {
                    methodInfo = method.MakeGenericMethod(methodBuilder.GetGenericArguments());
                }

                var local = Create(ilGen, instanceField, invokeCtor, methodInfo, parameterTypes);

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

            var type = typeBuilder.CreateType();

            assemblyBuilder.Save("SkyBuilding.Emit.dll");

            IEmit emit = (IEmit)Activator.CreateInstance(type, new Emit(), new Interceptor());

            emit.Test();

            var g1 = emit.TestG(new object());

            var g2 = emit.TestG(8);

            var g3 = emit.TestGConstraint(DateTimeKind.Utc);

            var i = emit.TestInt32(8);

            var v = emit.TestValueType(1);

            var c = emit.TestClas();
        }

        private static LocalBuilder Create(ILGenerator ilGen, FieldBuilder instanceField, ConstructorInfo invokeCtor, MethodInfo method, Type[] parameters)
        {
            var local = ilGen.DeclareLocal(typeof(IInvokeBinder));

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
                if (type.IsValueType)
                    ilGen.Emit(OpCodes.Box, type);

                //! 将参数存入数组。
                ilGen.Emit(OpCodes.Stelem_Ref);
            });

            ilGen.Emit(OpCodes.Stloc, array);

            //if (method.IsGenericMethod)
            //{
            //    var genericArguments = method.GetGenericArguments();

            //    var arguments = ilGen.DeclareLocal(typeof(Type[]));

            //    //? 数组长度入栈
            //    ilGen.Emit(OpCodes.Ldc_I4, genericArguments.Length);

            //    ilGen.Emit(OpCodes.Newarr, typeof(Type));

            //    genericArguments.ForEach((type, index) =>
            //    {
            //        ilGen.Emit(OpCodes.Dup);
            //        //? 数组下标入栈
            //        ilGen.Emit(OpCodes.Ldc_I4, index);
            //        //? 加载对应下标的参数。（第0个是this。）
            //        ilGen.Emit(OpCodes.Ldtoken, type);

            //        ilGen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));

            //        //! 将参数存入数组。
            //        ilGen.Emit(OpCodes.Stelem_Ref);
            //    });

            //    ilGen.Emit(OpCodes.Stloc, arguments);

            //    ilGen.Emit(OpCodes.Ldloc, methodLabel);

            //    ilGen.Emit(OpCodes.Ldloc, arguments);

            //    ilGen.Emit(OpCodes.Call, typeof(BaseProxy).GetMethod("MakeGenericMethod", BindingFlags.Public | BindingFlags.Static));

            //    //ilGen.Emit(OpCodes.Callvirt, typeof(MethodInfo).GetMethod("MakeGenericMethod", new Type[] { typeof(Type[]) }));

            //    ilGen.Emit(OpCodes.Stloc, methodLabel);
            //}

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, instanceField);

            ilGen.Emit(OpCodes.Ldtoken, method);

            ilGen.Emit(OpCodes.Ldtoken, method.DeclaringType);

            ilGen.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));

            ilGen.Emit(OpCodes.Castclass, typeof(MethodInfo));

            ilGen.Emit(OpCodes.Ldloc, array);

            //? 生成对象。
            ilGen.Emit(OpCodes.Newobj, invokeCtor);

            ilGen.Emit(OpCodes.Stloc, local);
            ilGen.Emit(OpCodes.Ldloc, local);

            return local;
        }
        private static Type[] AdjustGenericConstraints(MethodInfo methodToCopyGenericsFrom,
                                               GenericTypeParameterBuilder[] newGenericParameters,
                                               Type[] originalGenericArguments,
                                               Type[] constraints)
        {
            // HACK: the mono runtime has a strange bug where assigning to the constraints
            //       parameter and returning it throws, so we'll create a new array.
            //       System.ArrayTypeMismatchException : Source array type cannot be assigned to destination array type.
            Type[] adjustedConstraints = new Type[constraints.Length];
            for (var i = 0; i < constraints.Length; i++)
            {
                adjustedConstraints[i] = AdjustConstraintToNewGenericParameters(constraints[i],
                    methodToCopyGenericsFrom, originalGenericArguments, newGenericParameters);
            }
            return adjustedConstraints;
        }
        private static Type AdjustConstraintToNewGenericParameters(
            Type constraint, MethodInfo methodToCopyGenericsFrom, Type[] originalGenericParameters,
            GenericTypeParameterBuilder[] newGenericParameters)
        {
            if (constraint.IsGenericType)
            {
                var genericArgumentsOfConstraint = constraint.GetGenericArguments();

                for (var i = 0; i < genericArgumentsOfConstraint.Length; ++i)
                {
                    genericArgumentsOfConstraint[i] =
                        AdjustConstraintToNewGenericParameters(genericArgumentsOfConstraint[i], methodToCopyGenericsFrom,
                                                               originalGenericParameters, newGenericParameters);
                }
                return constraint.GetGenericTypeDefinition().MakeGenericType(genericArgumentsOfConstraint);
            }
            else if (constraint.IsGenericParameter)
            {
                // Determine the source of the parameter
                if (constraint.DeclaringMethod != null)
                {
                    // constraint comes from the method
                    var index = Array.IndexOf(originalGenericParameters, constraint);
                    Trace.Assert(index != -1,
                                 "When a generic method parameter has a constraint on another method parameter, both parameters must be declared on the same method.");
                    return newGenericParameters[index].AsType();
                }
                else // parameter from surrounding type
                {
                    Trace.Assert(constraint.DeclaringType.IsGenericTypeDefinition);
                    Trace.Assert(methodToCopyGenericsFrom.DeclaringType.IsGenericType
                                 && constraint.DeclaringType == methodToCopyGenericsFrom.DeclaringType.GetGenericTypeDefinition(),
                                 "When a generic method parameter has a constraint on a generic type parameter, the generic type must be the declaring typer of the method.");

                    var index = Array.IndexOf(constraint.DeclaringType.GetGenericArguments(), constraint);
                    Trace.Assert(index != -1, "The generic parameter comes from the given type.");
                    return methodToCopyGenericsFrom.DeclaringType.GetGenericArguments()[index]; // these are the actual, concrete types
                }
            }
            else
            {
                return constraint;
            }
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
