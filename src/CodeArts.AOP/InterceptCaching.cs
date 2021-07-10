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
    public static class InterceptCaching
    {
        private static readonly Type InterceptAttributeType = typeof(InterceptAttribute);

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

        public static void DefineMethodOverride(AstExpression instanceAst, ClassEmitter classEmitter, MethodInfo methodInfo, InterceptAttribute[] attributes)
        {
            var methodEmitter = classEmitter.DefineMethod(methodInfo.Name, methodInfo.Attributes, methodInfo.ReturnType);

            var parameterInfos = methodInfo.GetParameters();

            var paramterEmitters = new ParamterEmitter[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                paramterEmitters[i] = methodEmitter.DefineParameter(parameterInfos[i]);
            }

            if (attributes is null || attributes.Length == 0)
            {
                methodEmitter.Append(Call(instanceAst, methodInfo, paramterEmitters));

                goto label_core;
            }

            AstExpression[] arguments;

            var variable = methodEmitter.DeclareVariable(typeof(object[]));

            methodEmitter.Append(Assign(variable, Array(paramterEmitters)));

            if (methodInfo.IsGenericMethod)
            {
                arguments = new AstExpression[] { instanceAst, Constant(methodInfo), variable };
            }
            else
            {
                var fieldEmitter = classEmitter.DefineField($"____token__{methodInfo.Name}", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                classEmitter.TypeInitializer.Append(Assign(fieldEmitter, Constant(methodInfo)));

                arguments = new AstExpression[] { instanceAst, fieldEmitter, variable };
            }

            bool hasByRef = parameterInfos.Any(x => x.ParameterType.IsByRef);

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

                    finallyAst.Append(Assign(paramterEmitter, Convert(ArrayIndex(variable, i), paramterEmitter.ReturnType)));
                }

                blockAst.Append(Finally(finallyAst));
            }

            methodEmitter.Append(blockAst);

            InterceptCache.Add(methodInfo, attributes);

        label_core:

            foreach (var attributeData in methodInfo.GetCustomAttributesData())
            {
                if (InterceptAttributeType.IsAssignableFrom(attributeData.Constructor.ReflectedType))
                {
                    continue;
                }

                methodEmitter.SetCustomAttribute(attributeData);
            }

            classEmitter.DefineMethodOverride(methodEmitter, methodInfo);
        }
    }
}
