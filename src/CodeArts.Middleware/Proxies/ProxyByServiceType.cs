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

            var propertyMethods = new HashSet<MethodInfo>();

            var interceptMethods = InterceptCore.GetCustomAttributes(serviceType);

            foreach (var propertyInfo in serviceType.GetProperties())
            {
                var propertyEmitter = classEmitter.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType);

                if (propertyInfo.CanRead)
                {
                    var readMethod = propertyInfo.GetGetMethod(true);

                    propertyMethods.Add(readMethod);

                    propertyEmitter.SetGetMethod(InterceptCore.DefineMethodOverride(instanceAst, classEmitter, readMethod, new CustomAttributeData[0]));
                }

                if (propertyInfo.CanWrite)
                {
                    var writeMethod = propertyInfo.GetSetMethod(true);

                    propertyMethods.Add(writeMethod);

                    propertyEmitter.SetSetMethod(InterceptCore.DefineMethodOverride(instanceAst, classEmitter, writeMethod, new CustomAttributeData[0]));
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

            return Resolve(serviceType, classEmitter.CreateType(), lifetime);
        }

        private ServiceDescriptor ResolveIsClass()
        {
            string name = string.Concat(serviceType.Name, "Proxy");

            var interfaces = serviceType.GetInterfaces();

            var classEmitter = moduleEmitter.DefineType(name, TypeAttributes.Public | TypeAttributes.Class, serviceType, interfaces);

            var instanceAst = classEmitter.DefineField("____instance__", serviceType, FieldAttributes.Private | FieldAttributes.InitOnly | FieldAttributes.NotSerialized);

            bool throwsError = true;

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

            var propertyMethods = new HashSet<MethodInfo>();

            var interceptMethods = InterceptCore.GetCustomAttributes(serviceType);

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
                    InterceptCore.DefineMethodOverride(instanceAst, classEmitter, methodInfo, interceptAttributes);
                }
            }

            propertyMethods.Clear();

            interceptMethods.Clear();

            return Resolve(serviceType, classEmitter.CreateType(), lifetime);
        }

        protected abstract ServiceDescriptor Resolve(Type serviceType, Type implementationType, ServiceLifetime lifetime);
    }
}
