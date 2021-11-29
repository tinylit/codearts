using CodeArts.Emit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static CodeArts.Emit.AstExpression;

namespace CodeArts.Proxies
{
    class ProxyByImplementationType : IProxyByPattern
    {
        private static readonly Type InterceptAttributeType = typeof(InterceptAttribute);

        private readonly ModuleEmitter moduleEmitter;
        private readonly Type serviceType;
        private readonly Type implementationType;
        private readonly ServiceLifetime lifetime;

        public ProxyByImplementationType(ModuleEmitter moduleEmitter, Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            this.moduleEmitter = moduleEmitter;
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
            else if (implementationType.IsSealed)
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
            string name = string.Concat(serviceType.Name, "Proxy");

            var interfaces = serviceType.GetAllInterfaces();

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, null, interfaces);

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

            var propertyMethods = new HashSet<MethodInfo>();

            var interceptMethods = InterceptCore.GetCustomAttributes(implementationType);

            foreach (var propertyInfo in serviceType.GetProperties())
            {
                var propertyEmitter = classEmitter.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType);

                if (propertyInfo.CanRead)
                {
                    var getter = propertyInfo.GetGetMethod(true);

                    propertyMethods.Add(getter);

                    if (!interceptMethods.TryGetValue(getter, out var attributeDatas))
                    {
                        attributeDatas = new CustomAttributeData[0];
                    }

                    propertyEmitter.SetGetMethod(InterceptCore.DefineMethodOverride(instanceAst, classEmitter, getter, attributeDatas));
                }

                if (propertyInfo.CanWrite)
                {
                    var setter = propertyInfo.GetSetMethod(true);

                    propertyMethods.Add(setter);

                    if (!interceptMethods.TryGetValue(setter, out var attributeDatas))
                    {
                        attributeDatas = new CustomAttributeData[0];
                    }

                    propertyEmitter.SetSetMethod(InterceptCore.DefineMethodOverride(instanceAst, classEmitter, setter, attributeDatas));
                }
            }

            foreach (var methodInfo in serviceType.GetMethods())
            {
                if (propertyMethods.Contains(methodInfo))
                {
                    continue;
                }

                if (interceptMethods.TryGetValue(methodInfo, out var interceptAttributes))
                {
                    InterceptCore.DefineMethodOverride(instanceAst, classEmitter, methodInfo, interceptAttributes);
                }
                else
                {
                    InterceptCore.DefineMethodOverride(instanceAst, classEmitter, methodInfo, new CustomAttributeData[0]);
                }
            }

            propertyMethods.Clear();

            interceptMethods.Clear();

            return new ServiceDescriptor(serviceType, classEmitter.CreateType(), lifetime);
        }

        private ServiceDescriptor ResolveIsClass()
        {
            string name = string.Concat(serviceType.Name, "Proxy");

            var interfaces = implementationType.GetInterfaces();

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, implementationType, interfaces);

            foreach (var constructorInfo in implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var constructorEmitter = classEmitter.DefineConstructor(constructorInfo.Attributes);

                var parameterInfos = constructorInfo.GetParameters();
                var parameterEmiters = new ParameterEmitter[parameterInfos.Length];

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    parameterEmiters[i] = constructorEmitter.DefineParameter(parameterInfos[i]);
                }

                constructorEmitter.InvokeBaseConstructor(constructorInfo, parameterEmiters);
            }

            var propertyMethods = new HashSet<MethodInfo>();

            var interceptMethods = InterceptCore.GetCustomAttributes(implementationType);

            foreach (var propertyInfo in serviceType.GetProperties())
            {
                MethodInfo getter = null, setter = null;

                if (propertyInfo.CanRead)
                {
                    propertyMethods.Add(getter = propertyInfo.GetGetMethod(true));
                }

                if (propertyInfo.CanRead)
                {
                    propertyMethods.Add(setter = propertyInfo.GetSetMethod(true));
                }

                if ((getter is null || !interceptMethods.ContainsKey(getter)) 
                    && (setter is null || !interceptMethods.ContainsKey(setter)))
                {
                    continue;
                }

                var propertyEmitter = classEmitter.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType);

                if (propertyInfo.CanRead)
                {
                    if (!interceptMethods.TryGetValue(getter, out var attributeDatas))
                    {
                        attributeDatas = new CustomAttributeData[0];
                    }

                    propertyEmitter.SetGetMethod(InterceptCore.DefineMethodOverride(This, classEmitter, getter, attributeDatas));
                }

                if (propertyInfo.CanWrite)
                {

                    if (!interceptMethods.TryGetValue(setter, out var attributeDatas))
                    {
                        attributeDatas = new CustomAttributeData[0];
                    }

                    propertyEmitter.SetSetMethod(InterceptCore.DefineMethodOverride(This, classEmitter, setter, attributeDatas));
                }
            }

            foreach (var methodInfo in serviceType.GetMethods())
            {
                if (propertyMethods.Contains(methodInfo))
                {
                    continue;
                }

                if (interceptMethods.TryGetValue(methodInfo, out var interceptAttributes))
                {
                    InterceptCore.DefineMethodOverride(This, classEmitter, methodInfo, interceptAttributes);
                }
            }

            propertyMethods.Clear();

            interceptMethods.Clear();

            return new ServiceDescriptor(serviceType, classEmitter.CreateType(), lifetime);
        }
    }
}
