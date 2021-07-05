using CodeArts.Emit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using static CodeArts.Emit.AstExpression;

namespace CodeArts.Proxies
{
    class ProxyByType : IProxyByPattern
    {
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
                    var parameterEmiters = new AstExpression[parameterInfos.Length];

                    for (int i = 0; i < parameterInfos.Length; i++)
                    {
                        parameterEmiters[i] = constructorEmitter.DefineParameter(parameterInfos[i]);
                    }

                    constructorEmitter.Append(Assign(target, New(constructorInfo, parameterEmiters)));
                }
            }

            throw new NotImplementedException();
        }
    }
}
