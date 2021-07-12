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

        private static readonly MethodInfo InterceptMethodCall;
        private static readonly MethodInfo InterceptAsyncMethodCall;
        private static readonly MethodInfo InterceptAsyncGenericMethodCall;

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

                if (!x.DeclaringType.IsGenericType || !y.DeclaringType.IsGenericType)
                {
                    return false;
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

                if (x.ReflectedType != y.ReflectedType)
                {
                    if (x.ReflectedType.IsGenericType && y.ReflectedType.IsGenericType)
                    {
                        if (x.ReflectedType.GetGenericTypeDefinition() != y.ReflectedType.GetGenericTypeDefinition())
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                if (x.ReturnType != y.ReturnType)
                {
                    if (x.ReturnType.IsGenericType && y.ReturnType.IsGenericType)
                    {
                        if (x.ReturnType.GetGenericTypeDefinition() != y.ReturnType.GetGenericTypeDefinition())
                        {
                            return false;
                        }
                    }
                    else if (!x.ReturnType.IsGenericParameter)
                    {
                        return false;
                    }
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

            public int GetHashCode(MethodInfo obj) => obj is null ? 0 : obj.GetHashCode();
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

            if (InterceptCache.TryGetValue(context.Main.IsGenericMethod ? context.Main.GetGenericMethodDefinition() : context.Main, out var attributes))
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

            if (InterceptCache.TryGetValue(context.Main.IsGenericMethod ? context.Main.GetGenericMethodDefinition() : context.Main, out var attributes))
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

            if (InterceptCache.TryGetValue(context.Main.IsGenericMethod ? context.Main.GetGenericMethodDefinition() : context.Main, out var attributes))
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

            var methodEmitter = classEmitter.DefineMethodOverride(methodInfo, methodAttributes);

            var paramterEmitters = methodEmitter.GetParameters();

            if (attributes is null || attributes.Length == 0)
            {
                methodEmitter.Append(Call(instanceAst, methodInfo, paramterEmitters));

                goto label_core;
            }

            AstExpression[] arguments = null;

            var variable = methodEmitter.DeclareVariable(typeof(object[]));

            methodEmitter.Append(Assign(variable, Array(paramterEmitters)));

            if (methodEmitter.IsGenericMethod)
            {
                arguments = new AstExpression[] { instanceAst, Constant(methodInfo.MakeGenericMethod(methodEmitter.GetGenericArguments())), variable };
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

            methodEmitter.Append(blockAst);

            if (methodInfo.IsGenericMethod)
            {
                InterceptCache[methodInfo.GetGenericMethodDefinition()] = attributes;
            }
            else
            {
                InterceptCache[methodInfo] = attributes;
            }

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
