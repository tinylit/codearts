using SkyBuilding.AOP;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ConsoleEmit
{
    public interface IEmit
    {
        void Test(int i);
    }

    public class Emit : IEmit
    {
        public void Test(int i)
        {
        }
    }

    public class BaseProxy
    {
        public void Intercept(IInvokeBinder interceptor)
        {

        }
    }

    public class Interceptor : IInterceptor
    {
        public void Intercept(IInvokeBinder invokeBinder)
        {
            invokeBinder.Invoke();
        }

        public void Intercept(InvokeBinder invokeBinder)
        {

        }
    }

    public class InvokeBinder : IInvokeBinder
    {
        public InvokeBinder(object instance, MethodInfo method, object[] arguments)
        {
            Instance = instance;
            Method = method;
            Arguments = arguments ?? new object[0];
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

        public void SetArgumentValue(int index, object value) => Arguments[index < 0 ? Arguments.Length + index : index] = Valid(value, Method.GetParameters()[index < 0 ? Arguments.Length + index : index].ParameterType) ? value : throw new ArgumentException();

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

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("SkyBuilding.Module");

            var iEmitType = typeof(IEmit);

            var interfaceBuilder = moduleBuilder.DefineType("IEmitProxy", TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.NotPublic, null, new Type[] { iEmitType });

            var interfaceType = interfaceBuilder.CreateType();

            var interceptorType = typeof(IInterceptor);

            var typeBuilder = moduleBuilder.DefineType("EmitProxy", TypeAttributes.Class | TypeAttributes.NotPublic, typeof(BaseProxy), new Type[] { interfaceType });

            var instanceField = typeBuilder.DefineField("instance", iEmitType, FieldAttributes.Private | FieldAttributes.InitOnly);

            var interceptorField = typeBuilder.DefineField("interceptor", interceptorType, FieldAttributes.Private | FieldAttributes.InitOnly);

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { iEmitType, interceptorType });

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
            var returnValue = invokeType.GetProperty("ReturnValue");
            var invokeCtor = invokeType.GetConstructor(new Type[] { typeof(object), typeof(MethodInfo), typeof(object[]) });

            var interceptMethod = interceptorType.GetMethod("Intercept", new Type[] { typeof(IInvokeBinder) });

            foreach (var item in iEmitType.GetMethods())
            {
                var parameters = item.GetParameters().Select(x => x.ParameterType).ToArray();

                var methodBuilder = typeBuilder.DefineMethod(item.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                    item.ReturnType,
                    parameters);

                var ilGen = methodBuilder.GetILGenerator();

                var local = Create(ilGen, instanceField, invokeCtor, item, parameters);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, interceptorField);
                ilGen.Emit(OpCodes.Ldloc, local);
                ilGen.Emit(OpCodes.Call, interceptMethod);

                if (item.ReturnType == typeof(void))
                {
                    ilGen.Emit(OpCodes.Pop);
                }
                else
                {
                    ilGen.Emit(OpCodes.Callvirt, returnValue.GetMethod);

                    if (item.ReturnType.IsValueType)
                    {
                        ilGen.Emit(OpCodes.Unbox_Any, item.ReturnType);
                    }
                    else
                    {
                        ilGen.Emit(OpCodes.Castclass, item.ReturnType);
                    }

                    ilGen.Emit(OpCodes.Stloc_0);
                    ilGen.Emit(OpCodes.Ldloc_0);
                }

                ilGen.Emit(OpCodes.Ret);

                typeBuilder.DefineMethodOverride(methodBuilder, item);
            }

            //assemblyBuilder.Save("SkyBuilding.Emit.dll");

            var type = typeBuilder.CreateType();

            assemblyBuilder.Save("SkyBuilding.Emit.dll");

            IEmit emit = (IEmit)Activator.CreateInstance(type, new Emit(), new Interceptor());

            emit.Test(1);
        }

        private static LocalBuilder Create(ILGenerator ilGen, FieldBuilder instanceField, ConstructorInfo invokeCtor, MethodInfo method, Type[] parameters)
        {
            var local = ilGen.DeclareLocal(typeof(IInvokeBinder));

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, instanceField);

            ilGen.Emit(OpCodes.Ldtoken, method);

            ilGen.Emit(OpCodes.Ldtoken, method.DeclaringType);

            ilGen.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) }));
            ilGen.Emit(OpCodes.Castclass, typeof(MethodInfo));


            //! 声明一个类型为object的局部数组
            LocalBuilder array = ilGen.DeclareLocal(typeof(object[]));

            //? 数组长度入栈
            ilGen.Emit(OpCodes.Ldc_I4, parameters.Length);

            //! 生成一个object的数组。
            ilGen.Emit(OpCodes.Newarr, typeof(object));

            //? 赋值给局部数组变量
            ilGen.Emit(OpCodes.Stloc, array);

            for (int i = 0, length = parameters.Length; i < length; i++)
            {
                //? 数组入栈
                ilGen.Emit(OpCodes.Ldloc, array);
                //? 数组下标入栈
                ilGen.Emit(OpCodes.Ldc_I4, i);
                //? 加载对应下标的参数。（第0个是this。）
                ilGen.Emit(OpCodes.Ldarg, i + 1);

                //! 值类型参数装箱。
                if (parameters[i].IsValueType)
                    ilGen.Emit(OpCodes.Box, parameters[i]);

                //! 将参数存入数组。
                ilGen.Emit(OpCodes.Stelem_Ref);
            }

            ilGen.Emit(OpCodes.Ldloc, array);

            //? 生成对象。
            ilGen.Emit(OpCodes.Newobj, invokeCtor);

            ilGen.Emit(OpCodes.Stloc, local);
            ilGen.Emit(OpCodes.Ldloca_S, local);

            return local;
        }
    }

    public class Test
    {
        public Test(IInterceptor value)
        {

        }
    }
}
