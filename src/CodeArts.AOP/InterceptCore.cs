using CodeArts.AOP;
using CodeArts.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static CodeArts.Emit.AstExpression;

namespace CodeArts
{
    /// <summary>
    /// 拦截器。
    /// </summary>
    public static class InterceptCore
    {
        private static readonly Type InterceptAttributeType = typeof(InterceptAttribute);

        private static readonly Dictionary<MethodInfo, InterceptAttribute[]> InterceptCache = new Dictionary<MethodInfo, InterceptAttribute[]>();
        private static readonly Dictionary<Type, Dictionary<MethodInfo, InterceptAttribute[]>> GenericInterceptCache = new Dictionary<Type, Dictionary<MethodInfo, InterceptAttribute[]>>();

        private static readonly Type[] ContextTypes = new Type[] { typeof(object), typeof(MethodInfo), typeof(object[]) };

        private static readonly ConstructorInfo InterceptContextCtor = typeof(InterceptContext).GetConstructor(ContextTypes);
        private static readonly ConstructorInfo InterceptAsyncContextCtor = typeof(InterceptAsyncContext).GetConstructor(ContextTypes);

        private static readonly MethodInfo InterceptMethodCall;
        private static readonly MethodInfo InterceptAsyncMethodCall;
        private static readonly MethodInfo InterceptAsyncGenericMethodCall;

        static InterceptCore()
        {
            var methodInfos = typeof(InterceptCore).GetMethods();

            InterceptMethodCall = methodInfos.Single(x => x.Name == nameof(Intercept));

            InterceptAsyncMethodCall = methodInfos.Single(x => x.Name == nameof(InterceptAsync) && !x.IsGenericMethod);

            InterceptAsyncGenericMethodCall = methodInfos.Single(x => x.Name == nameof(InterceptAsync) && x.IsGenericMethod);
        }

        private static bool TryGetValue(MethodInfo methodInfo, out InterceptAttribute[] attributes)
        {
            if (methodInfo.IsGenericMethod)
            {
                methodInfo = methodInfo.GetGenericMethodDefinition();
            }

            if (methodInfo.DeclaringType.IsGenericType)
            {
                if (GenericInterceptCache.TryGetValue(methodInfo.DeclaringType.GetGenericTypeDefinition(), out Dictionary<MethodInfo, InterceptAttribute[]> interceptCache))
                {
                    return interceptCache.TryGetValue(methodInfo, out attributes);
                }

                attributes = new InterceptAttribute[0];

                return false;
            }

            return InterceptCache.TryGetValue(methodInfo, out attributes);
        }

        private static void InterceptAdd(MethodInfo methodInfo, InterceptAttribute[] attributes)
        {
            if (methodInfo.IsGenericMethod)
            {
                methodInfo = methodInfo.GetGenericMethodDefinition();
            }

            if (methodInfo.DeclaringType.IsGenericType)
            {
                var typeDefinition = methodInfo.DeclaringType.GetGenericTypeDefinition();

                if (!GenericInterceptCache.TryGetValue(typeDefinition, out Dictionary<MethodInfo, InterceptAttribute[]> interceptCache))
                {
                    GenericInterceptCache.Add(typeDefinition, interceptCache = new Dictionary<MethodInfo, InterceptAttribute[]>());
                }

                interceptCache[methodInfo] = attributes;
            }
            else
            {
                InterceptCache[methodInfo] = attributes;
            }
        }

        /// <summary>
        /// 拦截同步方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <returns></returns>
        public static object Intercept(InterceptContext context)
        {
            var intercept = new Intercept();

            if (TryGetValue(context.Main, out var attributes))
            {
                foreach (var attribute in attributes)
                {
                    intercept = new MiddlewareIntercept(attribute, intercept);
                }
            }

            intercept.Run(context);

            return context.ReturnValue;
        }

        /// <summary>
        /// 拦截异步方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <returns></returns>
        public static Task InterceptAsync(InterceptAsyncContext context)
        {
            var intercept = new InterceptAsync();

            if (TryGetValue(context.Main, out var attributes))
            {
                foreach (var attribute in attributes)
                {
                    intercept = new MiddlewareInterceptAsync(attribute, intercept);
                }
            }

            return intercept.RunAsync(context);
        }

        /// <summary>
        /// 拦截异步有返回值方法。
        /// </summary>
        /// <typeparam name="T">返回值类型。</typeparam>
        /// <param name="context">上下文。</param>
        /// <returns></returns>
        public static Task<T> InterceptAsync<T>(InterceptAsyncContext context)
        {
            var intercept = new InterceptAsync<T>();

            if (TryGetValue(context.Main, out var attributes))
            {
                foreach (var attribute in attributes)
                {
                    intercept = new MiddlewareInterceptAsync<T>(attribute, intercept);
                }
            }

            return intercept.RunAsync(context);
        }

        /// <summary>
        /// 重写方法。
        /// </summary>
        /// <param name="instanceAst">方法“this”上下文。</param>
        /// <param name="classEmitter">类。</param>
        /// <param name="methodInfo">被重写的方法。</param>
        /// <param name="attributes">拦截器。</param>
        public static void DefineMethodOverride(AstExpression instanceAst, ClassEmitter classEmitter, MethodInfo methodInfo, InterceptAttribute[] attributes)
        {
            var methodAttributes = methodInfo.Attributes;

            if ((methodAttributes & MethodAttributes.Abstract) == MethodAttributes.Abstract)
            {
                methodAttributes ^= MethodAttributes.Abstract;
            }

            var overrideEmitter = classEmitter.DefineMethodOverride(ref methodInfo, methodAttributes);

            var paramterEmitters = overrideEmitter.GetParameters();

            foreach (var attributeData in methodInfo.GetCustomAttributesData())
            {
                if (InterceptAttributeType.IsAssignableFrom(attributeData.Constructor.ReflectedType))
                {
                    continue;
                }

                overrideEmitter.SetCustomAttribute(attributeData);
            }

            if (attributes is null || attributes.Length == 0)
            {
                if (methodInfo.DeclaringType.IsGenericType)
                {
                    var fieldEmitter = classEmitter.DefineField($"____token__{methodInfo.Name}", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                    classEmitter.TypeInitializer.Append(Assign(fieldEmitter, Constant(methodInfo)));

                    overrideEmitter.Append(Invoke(instanceAst, fieldEmitter, Array(paramterEmitters)));
                }
                else
                {
                    overrideEmitter.Append(Call(instanceAst, methodInfo, paramterEmitters));
                }

                return;
            }

            AstExpression[] arguments = null;

            var variable = overrideEmitter.DeclareVariable(typeof(object[]));

            overrideEmitter.Append(Assign(variable, Array(paramterEmitters)));

            if (overrideEmitter.IsGenericMethod)
            {
                arguments = new AstExpression[] { instanceAst, Constant(methodInfo.MakeGenericMethod(overrideEmitter.GetGenericArguments())), variable };
            }
            else
            {
                var fieldEmitter = classEmitter.DefineField($"____token__{methodInfo.Name}", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                classEmitter.TypeInitializer.Append(Assign(fieldEmitter, Constant(methodInfo)));

                arguments = new AstExpression[] { instanceAst, fieldEmitter, variable };
            }

            bool hasByRef = paramterEmitters.Any(x => x.RuntimeType.IsByRef);

            var blockAst = hasByRef ? Try(methodInfo.ReturnType) : Block(methodInfo.ReturnType);

            if (methodInfo.ReturnType.IsClass && typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
            {
                if (methodInfo.ReturnType.IsGenericType)
                {
                    blockAst.Append(Call(InterceptAsyncGenericMethodCall.MakeGenericMethod(methodInfo.ReturnType.GetGenericArguments()), New(InterceptAsyncContextCtor, arguments)));
                }
                else
                {
                    blockAst.Append(Call(InterceptAsyncMethodCall, New(InterceptAsyncContextCtor, arguments)));
                }
            }
            else if (methodInfo.ReturnType == typeof(void))
            {
                blockAst.Append(Call(InterceptMethodCall, New(InterceptContextCtor, arguments)));
            }
            else
            {
                var value = blockAst.DeclareVariable(typeof(object));

                blockAst.Append(Assign(value, Call(InterceptMethodCall, New(InterceptContextCtor, arguments))));

                blockAst.Append(Condition(Equal(value, Constant(null)), Default(methodInfo.ReturnType), Convert(value, methodInfo.ReturnType)));
            }

            if (hasByRef)
            {
                var finallyAst = Block(typeof(void));

                for (int i = 0; i < paramterEmitters.Length; i++)
                {
                    var paramterEmitter = paramterEmitters[i];

                    if (!paramterEmitter.IsByRef)
                    {
                        continue;
                    }

                    finallyAst.Append(Assign(paramterEmitter, Convert(ArrayIndex(variable, i), paramterEmitter.RuntimeType)));
                }

                blockAst.Append(Finally(finallyAst));
            }

            overrideEmitter.Append(blockAst);

            InterceptAdd(methodInfo, attributes);
        }
    }
}
