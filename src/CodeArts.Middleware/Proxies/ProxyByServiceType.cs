using CodeArts.Emit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static CodeArts.Emit.AstExpression;

namespace CodeArts.Proxies
{
    abstract class ProxyByServiceType : IProxyByPattern
    {
        private static readonly Type InterceptAttributeType = typeof(InterceptAttribute);

        private readonly ModuleEmitter moduleEmitter;
        private readonly Type serviceType;
        private readonly ServiceLifetime lifetime;

        public ProxyByServiceType(ModuleEmitter moduleEmitter, Type serviceType, ServiceLifetime lifetime)
        {
            this.moduleEmitter = moduleEmitter;
            this.serviceType = serviceType;
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
            else if (serviceType.IsSealed)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类是密封类!");
            }
            else if (serviceType.IsValueType)
            {
                throw new NotSupportedException($"代理“{serviceType.FullName}”类是值类型!");
            }
            else
            {
                return ResolveIsClass();
            }
        }

        private ServiceDescriptor ResolveIsInterface()
        {
            string name = string.Concat(serviceType.Name, "Proxy");

            var interfaces = serviceType.GetAllInterfaces();

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, null, interfaces);

            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            var constructorEmitter = classEmitter.DefineConstructor(MethodAttributes.Public);

            var parameterEmitter = constructorEmitter.DefineParameter(serviceType, ParameterAttributes.None, "instance");

            constructorEmitter.Append(Assign(instanceAst, parameterEmitter));

            var interceptMethods = new Dictionary<MethodInfo, InterceptAttribute[]>(MethodInfoEqualityComparer.Instance);

            foreach (var type in interfaces)
            {
                bool flag = type.IsDefined(InterceptAttributeType, false);

                var attributes = flag
                    ? (InterceptAttribute[])type.GetCustomAttributes(InterceptAttributeType, false)
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
                    InterceptCore.DefineMethodOverride(instanceAst, classEmitter, methodInfo, interceptAttributes);
                }
                else
                {
                    InterceptCore.DefineMethodOverride(instanceAst, classEmitter, methodInfo, null);
                }
            }

            interceptMethods.Clear();

            return Resolve(serviceType, classEmitter.CreateType(), lifetime);
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

            return results;
        }

        private ServiceDescriptor ResolveIsClass()
        {
            string name = string.Concat(serviceType.Name, "Proxy");

            var interfaces = serviceType.GetInterfaces();

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, serviceType, interfaces);

            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            bool throwsError = true;

            var constructorInfos = serviceType.GetConstructors();

            foreach (var constructorInfo in serviceType.GetConstructors())
            {
                var parameterInfos = constructorInfo.GetParameters();

                if (parameterInfos.Length > 0)
                {
                    continue;
                }

                throwsError = false;

                var constructorEmitter = classEmitter.DefineConstructor(MethodAttributes.Public);

                var parameterEmitter = constructorEmitter.DefineParameter(serviceType, ParameterAttributes.None, "instance");

                constructorEmitter.Append(Assign(instanceAst, parameterEmitter));

                constructorEmitter.InvokeBaseConstructor(constructorInfo);

                break;
            }

            if (throwsError)
            {
                throw new AstException($"“{serviceType.FullName}”不存在无参构造函数!");
            }

            var interceptMethods = new Dictionary<MethodInfo, InterceptAttribute[]>(MethodInfoEqualityComparer.Instance);

            foreach (var type in interfaces)
            {
                bool flag = type.IsDefined(InterceptAttributeType, false);

                var attributes = flag
                    ? (InterceptAttribute[])type.GetCustomAttributes(InterceptAttributeType, false)
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

            var iterationType = serviceType;

            do
            {
                bool isDefined = iterationType.IsDefined(InterceptAttributeType, false);

                var intercepts = isDefined
                    ? (InterceptAttribute[])iterationType.GetCustomAttributes(InterceptAttributeType, false)
                    : new InterceptAttribute[0];

                foreach (var methodInfo in iterationType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    var interceptAttributes = methodInfo.IsDefined(InterceptAttributeType, false)
                        ? Merge(intercepts, (InterceptAttribute[])methodInfo.GetCustomAttributes(InterceptAttributeType, false))
                        : intercepts;

                    if (interceptAttributes.Length == 0)
                    {
                        continue;
                    }

                    if (interceptMethods.TryGetValue(methodInfo, out var attributes))
                    {
                        interceptMethods[methodInfo] = Merge(interceptAttributes, attributes);
                    }
                    else
                    {
                        interceptMethods.Add(methodInfo, interceptAttributes);
                    }
                }

                iterationType = iterationType.BaseType;

            } while (iterationType != null && iterationType != typeof(object));

            var methodInfos = serviceType.GetMethods();

            foreach (var methodInfo in methodInfos)
            {
                if (interceptMethods.TryGetValue(methodInfo, out var interceptAttributes))
                {
                    InterceptCore.DefineMethodOverride(instanceAst, classEmitter, methodInfo, interceptAttributes);
                }
            }

            return Resolve(serviceType, classEmitter.CreateType(), lifetime);
        }

        protected abstract ServiceDescriptor Resolve(Type serviceType, Type implementationType, ServiceLifetime lifetime);
    }
}
