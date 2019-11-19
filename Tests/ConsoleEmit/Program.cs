using SkyBuilding.AOP;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ConsoleEmit
{
    public interface IEmit
    {
        bool Test(int i);
    }

    public class Emit : IEmit
    {
        public bool Test(int i) => i > 0;
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
        public InvokeBinder(object instance, MethodInfo method, object[] arguments)
        {
            Instance = instance;
            Method = method;
            Arguments = arguments;
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
                .DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("SkyBuilding.Module");

            var iEmitType = typeof(IEmit);

            var interfaceBuilder = moduleBuilder.DefineType("IEmitProxy", TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.NotPublic, null, new Type[] { iEmitType });

            var interfaceType = interfaceBuilder.CreateType();

            var interceptorType = typeof(IInterceptor);

            var typeBuilder = moduleBuilder.DefineType("EmitProxy", TypeAttributes.Class | TypeAttributes.NotPublic, null, new Type[] { interfaceType, interceptorType });

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

                var methodBuilder = typeBuilder.DefineMethod(item.Name, MethodAttributes.Public, item.ReturnType, parameters);

                var ilGen = methodBuilder.GetILGenerator();

                var invokeBuilder = Create(ilGen, invokeCtor, item, parameters);

                ilGen.Emit(OpCodes.Call, interceptMethod);

                if (item.ReturnType != typeof(void))
                {
                    var result = ilGen.DeclareLocal(item.ReturnType);

                    ilGen.Emit(OpCodes.Stloc, result);

                    ilGen.Emit(OpCodes.Ldloc, invokeBuilder);
                    ilGen.Emit(OpCodes.Ldloc, result);
                    ilGen.Emit(OpCodes.Box, item.ReturnType);
                    ilGen.Emit(OpCodes.Call, returnValue.GetMethod);
                }

                ilGen.Emit(OpCodes.Ret);
            }

            var type = typeBuilder.CreateType();
        }

        private static LocalBuilder Create(ILGenerator ilGen, ConstructorInfo invokeCtor, MethodInfo method, Type[] parameters)
        {
            //! 声明一个类型为object的局部数组
            ilGen.DeclareLocal(typeof(object[]));

            //? 数组长度入栈
            ilGen.Emit(OpCodes.Ldc_I4, parameters.Length);

            //! 生成一个object的数组。
            ilGen.Emit(OpCodes.Newarr, typeof(object));

            //? 赋值给局部数组变量
            ilGen.Emit(OpCodes.Stloc_0);

            for (int i = 0, length = parameters.Length; i < length; i++)
            {
                //? 数组入栈
                ilGen.Emit(OpCodes.Ldloc_0);
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

            //? 加载当前对象。
            ilGen.Emit(OpCodes.Ldarg_0);

            //? 加载执行方法。
            ilGen.Emit(OpCodes.Ldfld, method);

            //? 上面生成的数组。
            ilGen.Emit(OpCodes.Ldloc_0);

            //? 生成对象。
            ilGen.Emit(OpCodes.Newobj, invokeCtor);

            //? 声明一个变量。
            var invokeBuilder = ilGen.DeclareLocal(invokeCtor.DeclaringType);

            //? 将对象放在内存中。
            ilGen.Emit(OpCodes.Stloc, invokeBuilder);

            return invokeBuilder;
        }
    }
}
