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
    static class InterceptCaching
    {
        private static readonly Dictionary<MethodInfo, InterceptAttribute[]> InterceptCache = new Dictionary<MethodInfo, InterceptAttribute[]>();

        private static readonly Type[] ContextTypes = new Type[] { typeof(object), typeof(MethodInfo), typeof(object[]) };

        private static readonly ConstructorInfo InterceptContextCtor = typeof(InterceptContext).GetConstructor(ContextTypes);
        private static readonly ConstructorInfo InterceptAsyncContextCtor = typeof(InterceptAsyncContext).GetConstructor(ContextTypes);

        public static readonly MethodInfo InterceptMethodCall;
        public static readonly MethodInfo InterceptAsyncMethodCall;
        public static readonly MethodInfo InterceptAsyncGenericMethodCall;

        static InterceptCaching()
        {
            var methodInfos = typeof(InterceptCaching).GetMethods();

            InterceptMethodCall = methodInfos.Single(x => x.Name == nameof(Intercept));

            InterceptAsyncMethodCall = methodInfos.Single(x => x.Name == nameof(InterceptAsync) && !x.IsGenericMethod);

            InterceptAsyncGenericMethodCall = methodInfos.Single(x => x.Name == nameof(InterceptAsync) && x.IsGenericMethod);
        }

        public static object Intercept(InterceptContext context)
        {
            var intercept = new Intercept();

            if (InterceptCache.TryGetValue(context.Main, out var attributes))
            {
                foreach (var attribute in attributes)
                {
                    intercept = new MiddlewareIntercept(attribute, intercept);
                }
            }

            intercept.Run(context);

            return context.ReturnValue;
        }

        public static Task InterceptAsync(InterceptAsyncContext context)
        {
            var intercept = new InterceptAsync();

            if (InterceptCache.TryGetValue(context.Main, out var attributes))
            {
                foreach (var attribute in attributes)
                {
                    intercept = new MiddlewareInterceptAsync(attribute, intercept);
                }
            }

            return intercept.RunAsync(context);
        }

        public static Task<T> InterceptAsync<T>(InterceptAsyncContext context)
        {
            var intercept = new InterceptAsync<T>();

            if (InterceptCache.TryGetValue(context.Main, out var attributes))
            {
                foreach (var attribute in attributes)
                {
                    intercept = new MiddlewareInterceptAsync<T>(attribute, intercept);
                }
            }

            return intercept.RunAsync(context);
        }

        public static void DefineMethodOverride(ClassEmitter classEmitter, MethodInfo methodInfo, InterceptAttribute[] attributes)
        {
            var methodEmitter = classEmitter.DefineMethod(methodInfo.Name, methodInfo.Attributes, methodInfo.ReturnType);

            var parameterInfos = methodInfo.GetParameters();

            var paramterEmitters = new ParamterEmitter[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                paramterEmitters[i] = methodEmitter.DefineParameter(parameterInfos[i]);
            }

            var variable = methodEmitter.DeclareVariable(typeof(object[]));

            AstExpression[] arguments = new AstExpression[] { This, Constant(methodInfo), variable };

            methodEmitter.Append(Assign(variable, Array(paramterEmitters)));

            if (methodInfo.ReturnType.IsClass && typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
            {
                if (methodInfo.ReturnType.IsGenericType)
                {
                    methodEmitter.Append(Return(Call(InterceptAsyncGenericMethodCall.MakeGenericMethod(methodInfo.ReturnType.GetGenericArguments()), New(InterceptAsyncContextCtor, arguments))));
                }
                else
                {
                    methodEmitter.Append(Return(Call(InterceptAsyncMethodCall, New(InterceptAsyncContextCtor, arguments))));
                }
            }
            else
            {
                var value = methodEmitter.DeclareVariable(typeof(object));

                methodEmitter.Append(Assign(value, Call(InterceptMethodCall, New(InterceptContextCtor, arguments))));
            }
        }
    }
}
