using CodeArts.Middleware;
using CodeArts.Emit;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CodeArts.Emit.Expressions;
using static CodeArts.Emit.AstExpression;

namespace CodeArts
{
    /// <summary>
    /// 拦截器。
    /// </summary>
    public static class InterceptCore
    {
        private static readonly Type InterceptAttributeType = typeof(InterceptAttribute);
        private static readonly Type NoninterceptAttributeType = typeof(NoninterceptAttribute);

        private static readonly Type[] ContextTypes = new Type[] { typeof(MethodInfo), typeof(object), typeof(MethodInfo), typeof(object[]) };

        private static readonly ConcurrentDictionary<MethodInfo, InterceptAttribute[]> InterceptCache = new ConcurrentDictionary<MethodInfo, InterceptAttribute[]>(MethodInfoEqualityComparer.Instance);
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<MethodInfo, InterceptAttribute[]>> GenericInterceptCache = new ConcurrentDictionary<Type, ConcurrentDictionary<MethodInfo, InterceptAttribute[]>>();

        private static readonly ConstructorInfo InterceptContextCtor = typeof(NestedInterceptContext).GetConstructor(ContextTypes);

        private static readonly MethodInfo InterceptMethodCall;
        private static readonly MethodInfo InterceptGenericMethodCall;
        private static readonly MethodInfo InterceptAsyncMethodCall;
        private static readonly MethodInfo InterceptAsyncGenericMethodCall;

        /// <summary>
        /// IL 使用，不对外使用。
        /// </summary>
        public sealed class NestedInterceptContext : InterceptContext
        {
            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            public NestedInterceptContext(MethodInfo destinationProxyMethod, object target, MethodInfo main, object[] inputs) : base(target, main, inputs)
            {
                DestinationProxyMethod = destinationProxyMethod;
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            public MethodInfo DestinationProxyMethod { get; }
        }

        static InterceptCore()
        {
            var methodInfos = typeof(InterceptCore).GetMethods();

            InterceptMethodCall = methodInfos.Single(x => x.Name == nameof(Intercept) && !x.IsGenericMethod);

            InterceptGenericMethodCall = methodInfos.Single(x => x.Name == nameof(Intercept) && x.IsGenericMethod);

            InterceptAsyncMethodCall = methodInfos.Single(x => x.Name == nameof(InterceptAsync) && !x.IsGenericMethod);

            InterceptAsyncGenericMethodCall = methodInfos.Single(x => x.Name == nameof(InterceptAsync) && x.IsGenericMethod);
        }

        private static IList<CustomAttributeData> Merge(IList<CustomAttributeData> arrays, IList<CustomAttributeData> arrays2)
        {
            if (arrays.Count == 0)
            {
                return arrays2;
            }

            if (arrays2.Count == 0)
            {
                return arrays;
            }

            return arrays
                .Union(arrays2)
#if NET40
                .Where(x => InterceptAttributeType.IsAssignableFrom(x.Constructor.DeclaringType))
#else
                .Where(x => InterceptAttributeType.IsAssignableFrom(x.AttributeType))
#endif
                .ToList();
        }

        /// <summary>
        /// 获取拦截方法和拦截器信息。
        /// </summary>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        public static Dictionary<MethodInfo, IList<CustomAttributeData>> GetCustomAttributes(Type implementationType)
        {
            Type[] implementationTypes;

            if (implementationType.IsInterface)
            {
                implementationTypes = implementationType.GetAllInterfaces();
            }
            else
            {
                Type type = implementationType;

                List<Type> types = new List<Type>();

                do
                {
                    types.Add(type);

                } while ((type = type.BaseType) != null);

                types.AddRange(implementationType.GetInterfaces());

                implementationTypes = types.ToArray();
            }

            var noninterceptMethods = new HashSet<MethodInfo>(MethodInfoEqualityComparer.Instance);

            var interceptMethods = new Dictionary<MethodInfo, IList<CustomAttributeData>>(MethodInfoEqualityComparer.Instance);

            foreach (var type in implementationTypes)
            {
                bool flag = type.IsDefined(InterceptAttributeType, false);

                var attributes = flag
                    ? type.GetCustomAttributesData()
                    : new CustomAttributeData[0];

                foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (noninterceptMethods.Contains(methodInfo))
                    {
                        continue;
                    }

                    if (methodInfo.IsDefined(NoninterceptAttributeType, false))
                    {
                        noninterceptMethods.Add(methodInfo);

                        continue;
                    }

                    var interceptAttributes = methodInfo.IsDefined(InterceptAttributeType, false)
                        ? Merge(attributes, methodInfo.GetCustomAttributesData())
                        : attributes;

                    if (interceptAttributes.Count == 0)
                    {
                        continue;
                    }

                    if (interceptMethods.TryGetValue(methodInfo, out var intercepts))
                    {
                        interceptMethods[methodInfo] = Merge(intercepts, interceptAttributes);
                    }
                    else
                    {
                        interceptMethods.Add(methodInfo, interceptAttributes);
                    }
                }
            }

            noninterceptMethods.Clear();

            return interceptMethods;
        }

        private static InterceptAttribute[] GetInterceptAttributes(MethodInfo methodInfo)
        {
            if (methodInfo.DeclaringType.IsGenericType)
            {
                return GenericInterceptCache.GetOrAdd(methodInfo.DeclaringType.GetGenericTypeDefinition(), x => new ConcurrentDictionary<MethodInfo, InterceptAttribute[]>(MethodInfoEqualityComparer.Instance))
                    .GetOrAdd(methodInfo, x => (InterceptAttribute[])x.GetCustomAttributes(InterceptAttributeType, true));
            }

            return InterceptCache.GetOrAdd(methodInfo, x => (InterceptAttribute[])x.GetCustomAttributes(InterceptAttributeType, true));
        }

        /// <summary>
        /// 拦截同步方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <returns></returns>
        public static void Intercept(NestedInterceptContext context)
        {
            var intercept = new Intercept();

            foreach (InterceptAttribute attribute in GetInterceptAttributes(context.DestinationProxyMethod))
            {
                intercept = new MiddlewareIntercept(attribute, intercept);
            }

            intercept.Run(context);
        }

        /// <summary>
        /// 拦截同步方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <returns></returns>
        public static T Intercept<T>(NestedInterceptContext context)
        {
            var intercept = new Intercept<T>();

            foreach (InterceptAttribute attribute in GetInterceptAttributes(context.DestinationProxyMethod))
            {
                intercept = new MiddlewareIntercept<T>(attribute, intercept);
            }

            return intercept.Run(context);
        }

        /// <summary>
        /// 拦截异步方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <returns></returns>
        public static Task InterceptAsync(NestedInterceptContext context)
        {
            var intercept = new InterceptAsync();

            foreach (InterceptAttribute attribute in GetInterceptAttributes(context.DestinationProxyMethod))
            {
                intercept = new MiddlewareInterceptAsync(attribute, intercept);
            }

            return intercept.RunAsync(context);
        }

        /// <summary>
        /// 拦截异步有返回值方法。
        /// </summary>
        /// <typeparam name="T">返回值类型。</typeparam>
        /// <param name="context">上下文。</param>
        /// <returns></returns>
        public static Task<T> InterceptAsync<T>(NestedInterceptContext context)
        {
            var intercept = new InterceptAsync<T>();

            foreach (InterceptAttribute attribute in GetInterceptAttributes(context.DestinationProxyMethod))
            {
                intercept = new MiddlewareInterceptAsync<T>(attribute, intercept);
            }

            return intercept.RunAsync(context);
        }

        /// <summary>
        /// 重写方法。
        /// </summary>
        /// <param name="instanceAst">方法“this”上下文。</param>
        /// <param name="classEmitter">类。</param>
        /// <param name="methodInfo">被重写的方法。</param>
        /// <param name="attributeDatas">属性。</param>
        public static MethodEmitter DefineMethodOverride(AstExpression instanceAst, ClassEmitter classEmitter, MethodInfo methodInfo, IList<CustomAttributeData> attributeDatas)
        {
            bool flag = true;

            var overrideEmitter = classEmitter.DefineMethodOverride(ref methodInfo);

            var paramterEmitters = overrideEmitter.GetParameters();

#if NET40
            foreach (var attributeData in attributeDatas.Where(x => InterceptAttributeType.IsAssignableFrom(x.Constructor.DeclaringType)))
#else
            foreach (var attributeData in attributeDatas.Where(x => InterceptAttributeType.IsAssignableFrom(x.AttributeType)))
#endif
            {
                if (flag)
                {
                    flag = false;
                }

                overrideEmitter.SetCustomAttribute(attributeData);
            }

            if (flag && !methodInfo.DeclaringType.IsInterface && !methodInfo.IsDefined(InterceptAttributeType, true))
            {
                overrideEmitter.Append(Call(instanceAst, methodInfo, paramterEmitters));

                return overrideEmitter;
            }

            AstExpression[] arguments = null;

            var variable = Variable(typeof(object[]));

            overrideEmitter.Append(Assign(variable, Array(paramterEmitters)));

            if (overrideEmitter.IsGenericMethod)
            {
                arguments = new AstExpression[] { Constant(overrideEmitter), instanceAst, Constant(methodInfo), variable };
            }
            else
            {
                var tokenEmitter = classEmitter.DefineField($"____token__{methodInfo.Name}", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                classEmitter.TypeInitializer.Append(Assign(tokenEmitter, Constant(methodInfo)));

                var proxyEmitter = classEmitter.DefineField($"____proxy__{methodInfo.Name}", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                classEmitter.TypeInitializer.Append(Assign(proxyEmitter, Constant(overrideEmitter)));

                arguments = new AstExpression[] { proxyEmitter, instanceAst, tokenEmitter, variable };
            }

            BlockAst blockAst;

            if (paramterEmitters.Any(x => x.RuntimeType.IsByRef))
            {
                var finallyAst = Block(typeof(void));

                for (int i = 0; i < paramterEmitters.Length; i++)
                {
                    var paramterEmitter = paramterEmitters[i];

                    if (!paramterEmitter.IsByRef)
                    {
                        continue;
                    }

                    finallyAst.Append(Assign(paramterEmitter, Convert(ArrayIndex(variable, i), paramterEmitter.ParameterType)));
                }

                blockAst = Try(methodInfo.ReturnType, finallyAst);
            }
            else
            {
                blockAst = Block(methodInfo.ReturnType);
            }

            if (overrideEmitter.ReturnType.IsClass && typeof(Task).IsAssignableFrom(overrideEmitter.ReturnType))
            {
                if (overrideEmitter.ReturnType.IsGenericType)
                {
                    blockAst.Append(Call(InterceptAsyncGenericMethodCall.MakeGenericMethod(overrideEmitter.ReturnType.GetGenericArguments()), New(InterceptContextCtor, arguments)));
                }
                else
                {
                    blockAst.Append(Call(InterceptAsyncMethodCall, New(InterceptContextCtor, arguments)));
                }
            }
            else if (overrideEmitter.ReturnType == typeof(void))
            {
                blockAst.Append(Call(InterceptMethodCall, New(InterceptContextCtor, arguments)));
            }
            else
            {
                blockAst.Append(Call(InterceptGenericMethodCall.MakeGenericMethod(overrideEmitter.ReturnType), New(InterceptContextCtor, arguments)));
            }

            overrideEmitter.Append(blockAst);

            return overrideEmitter;
        }
    }
}
