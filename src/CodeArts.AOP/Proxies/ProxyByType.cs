using CodeArts.Emit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static CodeArts.Emit.AstExpression;

namespace CodeArts.Proxies
{
    public class ProxyByType : IProxyByPattern
    {
        private static readonly Type InterceptAttributeType = typeof(InterceptAttribute);

        private readonly Type serviceType;
        private readonly Type implementationType;
        private readonly ServiceLifetime lifetime;

        public ProxyByType(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            this.serviceType = serviceType;
            this.implementationType = implementationType;
            this.lifetime = lifetime;
        }

        public ServiceDescriptor Resolve()
        {
            if (serviceType.IsSealed)
            {
                throw new NotSupportedException("无法代理密封类!");
            }

            if (serviceType.IsInterface)
            {
                return ResolveIsInterface();
            }
            else if (implementationType.IsNested)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是密封类!");
            }
            else if (implementationType.IsInterface)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是接口!");
            }
            else if (implementationType.IsAbstract)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是抽象类!");
            }
            else if (implementationType.IsValueType)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是值类型!");
            }
            else
            {
                return ResolveIsClass();
            }
        }

        private ServiceDescriptor ResolveIsInterface()
        {
#if NET40_OR_GREATER
            var moduleEmitter = new ModuleEmitter(true);
#else
            var moduleEmitter = new ModuleEmitter();
#endif

            string name = string.Concat(serviceType.Name, "Proxy");

            var interfaces = serviceType.GetAllInterfaces();

            var classEmitter = new ClassEmitter(moduleEmitter, name, TypeAttributes.Public | TypeAttributes.Class, null, interfaces);

            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            foreach (var constructorInfo in implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var constructorEmitter = classEmitter.DefineConstructor(constructorInfo.Attributes);

                var parameterInfos = constructorInfo.GetParameters();
                var parameterEmiters = new AstExpression[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameterEmiters[i] = constructorEmitter.DefineParameter(parameterInfos[i]);
                }

                constructorEmitter.Append(Assign(instanceAst, Convert(New(constructorInfo, parameterEmiters), serviceType)));
            }

            var interceptMethods = new Dictionary<MethodInfo, InterceptAttribute[]>(MethodInfoEqualityComparer.Instance);

            foreach (var type in interfaces)
            {
                bool flag = type.IsDefined(InterceptAttributeType, false);

                var attributes = flag
                    ? (InterceptAttribute[])implementationType.GetCustomAttributes(InterceptAttributeType, false)
                    : new InterceptAttribute[0];

                foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var interceptAttributes = methodInfo.IsDefined(InterceptAttributeType, false)
                        ? Merge(attributes, (InterceptAttribute[])methodInfo.GetCustomAttributes(InterceptAttributeType, false))
                        : attributes;

                    if (interceptAttributes.Length == 0)
                    {
                        continue;
                    }

                    if (interceptMethods.ContainsKey(methodInfo))
                    {
                        throw new NotSupportedException($"{methodInfo.ReturnType.Name} “{methodInfo.Name}({string.Join(",", methodInfo.GetParameters().Select(x => string.Concat(x.ParameterType.Name, " ", x.Name)))})”重复定义!");
                    }
                    else
                    {
                        interceptMethods.Add(methodInfo, interceptAttributes);
                    }
                }
            }

            foreach (var methodInfo in serviceType.GetMethods())
            {
                if (interceptMethods.TryGetValue(methodInfo, out var interceptAttributes))
                {
                    InterceptCaching.DefineMethodOverride(instanceAst, classEmitter, methodInfo, interceptAttributes);
                }
                else
                {
                    InterceptCaching.DefineMethodOverride(instanceAst, classEmitter, methodInfo, null);
                }
            }

            var typeNew = classEmitter.CreateType();

#if NET40_OR_GREATER
            moduleEmitter.SaveAssembly();
#endif

            return new ServiceDescriptor(serviceType, typeNew, lifetime);
        }

        private static T[] Merge<T>(T[] arrays, T[] arrays2)
        {
            if (arrays.Length == 0)
            {
                return arrays2;
            }

            if (arrays2.Length == 0)
            {
                return arrays;
            }

            var results = new T[arrays.Length + arrays2.Length];

            System.Array.Copy(arrays, results, arrays.Length);

            System.Array.Copy(arrays2, 0, results, arrays.Length, arrays2.Length);

            return arrays;
        }

        private ServiceDescriptor ResolveIsClass()
        {
            var moduleEmitter = new ModuleEmitter();
            string name = string.Concat(serviceType.Name, "Proxy");

            var classEmitter = serviceType.IsInterface
                ? new ClassEmitter(moduleEmitter, name, TypeAttributes.Public | TypeAttributes.Sealed, null, serviceType.GetAllInterfaces())
                : new ClassEmitter(moduleEmitter, name, serviceType.Attributes | TypeAttributes.Sealed, implementationType, implementationType.GetInterfaces());

            var target = classEmitter.DefineField("____target__", implementationType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            if (serviceType.IsInterface)
            {
                foreach (var constructorInfo in implementationType.GetConstructors())
                {
                    var constructorEmitter = classEmitter.DefineConstructor(constructorInfo.Attributes);

                    var parameterInfos = constructorInfo.GetParameters();
                    var parameterEmiters = new AstExpression[parameterInfos.Length];

                    for (int i = 0; i < parameterInfos.Length; i++)
                    {
                        parameterEmiters[i] = constructorEmitter.DefineParameter(parameterInfos[i]);
                    }

                    constructorEmitter.Append(Assign(target, New(constructorInfo, parameterEmiters)));
                }
            }
            else if (implementationType.IsNested)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是密封类!");
            }
            else if (implementationType.IsInterface)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是接口!");
            }
            else if (implementationType.IsAbstract)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是抽象类!");
            }
            else if (implementationType.IsValueType)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类的实现类（“{implementationType.FullName}”）是值类型!");
            }
            else
            {
                foreach (var constructorInfo in implementationType.GetConstructors())
                {
                    var constructorEmitter = classEmitter.DefineConstructor(constructorInfo.Attributes);

                    var parameterInfos = constructorInfo.GetParameters();
                    var parameterEmiters = new ParamterEmitter[parameterInfos.Length];

                    for (int i = 0; i < parameterInfos.Length; i++)
                    {
                        parameterEmiters[i] = constructorEmitter.DefineParameter(parameterInfos[i]);
                    }

                    constructorEmitter.InvokeBaseConstructor(constructorInfo, parameterEmiters);
                }
            }

            throw new NotImplementedException();
        }
    }
}
