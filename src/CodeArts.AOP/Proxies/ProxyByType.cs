using CodeArts.Emit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static CodeArts.Emit.AstExpression;

namespace CodeArts.Proxies
{
    class ProxyByType : IProxyByPattern
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
                if (implementationType.IsDefined(InterceptAttributeType, false))
                {
                    if (implementationType.IsNested)
                    {
                        throw new NotSupportedException($"代理“{implementationType.FullName}”类是密封类!");
                    }

                    return ResolveIsInterfaceByClass();
                }

                foreach (var methodInfo in implementationType.GetMethods())
                {
                    if (methodInfo.IsDefined(InterceptAttributeType, false))
                    {
                        if (implementationType.IsNested)
                        {
                            throw new NotSupportedException($"代理“{implementationType.FullName}”类是密封类!");
                        }

                        if (!methodInfo.IsVirtual)
                        {
                            throw new NotSupportedException($"代理“{implementationType.FullName}”类的“{methodInfo.Name}({string.Join(",", methodInfo.GetParameters().Select(x => string.Concat(x.ParameterType.Name, " ", x.Name)))})”不是虚方法!");
                        }

                        return ResolveIsInterfaceByClass();
                    }
                }

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
            var moduleEmitter = new ModuleEmitter();
            string name = string.Concat(serviceType.Name, "Proxy");

            var classEmitter = new ClassEmitter(moduleEmitter, name, TypeAttributes.Public | TypeAttributes.Sealed, null, serviceType.GetAllInterfaces());

            var target = classEmitter.DefineField("____target__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            foreach (var constructorInfo in implementationType.GetConstructors())
            {
                var constructorEmitter = classEmitter.DefineConstructor(constructorInfo.Attributes);

                var parameterInfos = constructorInfo.GetParameters();
                var parameterEmiters = new AstExpression[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameterEmiters[i] = constructorEmitter.DefineParameter(parameterInfos[i]);
                }

                constructorEmitter.Append(Assign(target, Convert(New(constructorInfo, parameterEmiters), serviceType)));
            }

            bool flag = implementationType.IsDefined(InterceptAttributeType, false);

            var attributes = (InterceptAttribute[])implementationType.GetCustomAttributes(InterceptAttributeType, false);

            foreach (var methodInfo in implementationType.GetMethods())
            {
                if (methodInfo.IsDefined(InterceptAttributeType, false))
                {
                    if (!methodInfo.IsVirtual || methodInfo.IsStatic)
                    {
                        throw new NotSupportedException($"代理“{implementationType.FullName}”类的“{methodInfo.Name}({string.Join(",", methodInfo.GetParameters().Select(x => string.Concat(x.ParameterType.Name, " ", x.Name)))})”不是虚方法!");
                    }

                    var interceptAttributes = (InterceptAttribute[])methodInfo.GetCustomAttributes(InterceptAttributeType, false);

                    if (flag && methodInfo.DeclaringType == implementationType)
                    {
                        DefineMethodOverride(classEmitter, methodInfo, attributes.Union(interceptAttributes).ToArray());
                    }
                    else
                    {
                        DefineMethodOverride(classEmitter, methodInfo, interceptAttributes);
                    }

                    continue;
                }

                if (flag && methodInfo.DeclaringType == implementationType)
                {
                    DefineMethodOverride(classEmitter, methodInfo, attributes);

                    continue;
                }

                continue;
            }

            throw new NotImplementedException();
        }


        private static void DefineMethodOverride(ClassEmitter classEmitter, MethodInfo methodInfo, InterceptAttribute[] attributes)
        {
            var methodEmitter = classEmitter.DefineMethod(methodInfo.Name, methodInfo.Attributes, methodInfo.ReturnType);

            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                methodEmitter.DefineParameter(parameterInfo);
            }
        }

        private ServiceDescriptor ResolveIsInterfaceByClass()
        {
            var moduleEmitter = new ModuleEmitter();
            string name = string.Concat(serviceType.Name, "Proxy");

            var classEmitter = new ClassEmitter(moduleEmitter, name, TypeAttributes.Public | TypeAttributes.Sealed, implementationType, implementationType.GetInterfaces());

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

            throw new NotImplementedException();
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
