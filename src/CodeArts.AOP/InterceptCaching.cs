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

        private static readonly Dictionary<MethodInfo, InterceptAttribute[]> InterceptCache = new Dictionary<MethodInfo, InterceptAttribute[]>(MethodInfoEqualityComparer.Instance);

        private static readonly Type[] ContextTypes = new Type[] { typeof(object), typeof(MethodInfo), typeof(object[]) };

        private static readonly ConstructorInfo InterceptContextCtor = typeof(InterceptContext).GetConstructor(ContextTypes);
        private static readonly ConstructorInfo InterceptAsyncContextCtor = typeof(InterceptAsyncContext).GetConstructor(ContextTypes);

        public static readonly MethodInfo InterceptMethodCall;
        public static readonly MethodInfo InterceptAsyncMethodCall;
        public static readonly MethodInfo InterceptAsyncGenericMethodCall;

        private class MethodInfoEqualityComparer : IEqualityComparer<MethodInfo>
        {
            private MethodInfoEqualityComparer() { }

            public static MethodInfoEqualityComparer Instance = new MethodInfoEqualityComparer();
            public bool Equals(MethodInfo x, MethodInfo y)
            {
                if (x is null)
                {
                    return y is null;
                }

                if (y is null)
                {
                    return false;
                }

                if (x == y)
                {
                    return true;
                }

                if (!x.IsGenericMethod || !y.IsGenericMethod)
                {
                    return false;
                }

                if (x.DeclaringType != y.DeclaringType)
                {
                    return false;
                }

                if (!string.Equals(x.Name, y.Name))
                {
                    return false;
                }

                if (x.ReturnType != y.ReturnType && !x.ReturnType.IsGenericParameter)
                {
                    return false;
                }

                if (x.GetGenericArguments().Length != y.GetGenericArguments().Length)
                {
                    return false;
                }

                var xParameters = x.GetParameters();
                var yParameters = y.GetParameters();

                if (xParameters.Length != yParameters.Length)
                {
                    return false;
                }

                return xParameters
                .Zip(yParameters, (x1, y1) => x1.ParameterType == y1.ParameterType || !x1.ParameterType.IsGenericParameter)
                .All(isEquals => isEquals);
            }

            public int GetHashCode(MethodInfo obj) => obj?.Name.GetHashCode() ?? 0;
        }
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
            var methodAttributes = methodInfo.Attributes;

            if ((methodAttributes & MethodAttributes.Abstract) == MethodAttributes.Abstract)
            {
                methodAttributes ^= MethodAttributes.Abstract;
            }

            var methodEmitter = classEmitter.DefineMethod(methodInfo.Name, methodAttributes, methodInfo.ReturnType);

            var methodOverride = methodEmitter.OverrideFrom(methodInfo);

            var paramterEmitters = methodEmitter.GetParameters();

            if (attributes is null || attributes.Length == 0)
            {
                methodEmitter.Append(Call(instanceAst, methodInfo, paramterEmitters));

                goto label_core;
            }

            AstExpression[] arguments = null;

            var variable = methodEmitter.DeclareVariable(typeof(object[]));

            methodEmitter.Append(Assign(variable, Array(paramterEmitters)));

            if (methodOverride.IsGenericMethod)
            {
                arguments = new AstExpression[] { instanceAst, Constant(methodOverride), variable };
            }
            else
            {
                var fieldEmitter = classEmitter.DefineField($"____token__{methodOverride.Name}", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);

                classEmitter.TypeInitializer.Append(Assign(fieldEmitter, Constant(methodOverride)));

                arguments = new AstExpression[] { instanceAst, fieldEmitter, variable };
            }


            bool hasByRef = paramterEmitters.Any(x => x.RuntimeType.IsByRef);

            var blockAst = hasByRef ? Try(methodOverride.ReturnType) : Block(methodOverride.ReturnType);

            if (methodOverride.ReturnType.IsClass && typeof(Task).IsAssignableFrom(methodOverride.ReturnType))
            {
                if (methodOverride.ReturnType.IsGenericType)
                {
                    blockAst.Append(Call(InterceptAsyncGenericMethodCall.MakeGenericMethod(methodOverride.ReturnType.GetGenericArguments()), New(InterceptAsyncContextCtor, arguments)));
                }
                else
                {
                    blockAst.Append(Call(InterceptAsyncMethodCall, New(InterceptAsyncContextCtor, arguments)));
                }
            }
            else if (methodOverride.ReturnType == typeof(void))
            {
                blockAst.Append(Call(InterceptMethodCall, New(InterceptContextCtor, arguments)));
            }
            else
            {
                var value = blockAst.DeclareVariable(typeof(object));

                blockAst.Append(Assign(value, Call(InterceptMethodCall, New(InterceptContextCtor, arguments))));

                blockAst.Append(Condition(Equal(value, Constant(null)), Default(methodOverride.ReturnType), Convert(value, methodOverride.ReturnType)));
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
        }
    }
}
