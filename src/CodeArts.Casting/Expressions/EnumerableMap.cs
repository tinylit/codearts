using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Casting.Expressions
{
    using static Expression;

    /// <summary>
    /// 迭代器。
    /// </summary>
    public class EnumerableMap : MapExpression
    {
        private readonly static Type EnumerableType = typeof(IEnumerable);
        private readonly static Type EnumeratorType = typeof(IEnumerator);
        private readonly static MethodInfo GetEnumeratorFn = EnumerableType.GetMethod("GetEnumerator", Type.EmptyTypes);
        private readonly static MethodInfo MoveNextFn = EnumeratorType.GetMethod("MoveNext", Type.EmptyTypes);
        private readonly static PropertyInfo CurrentPropertyInfo = EnumeratorType.GetProperty("Current");
        private readonly static MethodInfo ToArrayFn = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray), MapExtensions.StaticFlags);

        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type conversionType)
        {
            return sourceType != typeof(string)
                && conversionType != typeof(string)
                && (sourceType.IsArray || EnumerableType.IsAssignableFrom(sourceType))
                && (conversionType.IsArray || EnumerableType.IsAssignableFrom(conversionType));
        }

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceType">原类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="configuration">配置文件。</param>
        /// <param name="sourceExpression">数据源表达式。</param>
        /// <returns></returns>
        protected override Expression ToSolve(Type sourceType, Type conversionType, IMapConfiguration configuration, Expression sourceExpression)
        {
            if (conversionType.IsArray)
            {
                if (conversionType.GetArrayRank() > 1)
                {
                    throw new NotSupportedException($"暂不支持映射到多维数组({conversionType})!");
                }
            }

            if (sourceType.IsArray)
            {
                if (sourceType.GetArrayRank() > 1)
                {
                    throw new NotSupportedException($"暂不支持多维数组({conversionType})的映射!");
                }
            }

            if (sourceType.IsArray)
            {
                if (conversionType.IsArray)
                {
                    if (configuration.AllowNullMapping == true || sourceType.IsValueType && !sourceType.IsNullable())
                    {
                        return ArrayToArray(conversionType, configuration, sourceExpression);
                    }
                }

                return ArrayTo(conversionType, configuration, sourceExpression);
            }

            foreach (var interfaceType in sourceType.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return EnumerableGenericTo(interfaceType, conversionType, configuration, sourceExpression);
                }
            }

            return EnumerableTo(conversionType, configuration, sourceExpression);
        }

        private static Expression ArrayToArray(Type conversionType, IMapConfiguration configuration, Expression sourceExpression)
        {
            var indexExp = Variable(typeof(int));

            var lengthExp = Variable(typeof(int));

            var elementType = conversionType.GetElementType();

            var variableExp = Variable(conversionType);

            LabelTarget break_label = Label(typeof(void));
            LabelTarget continue_label = Label(typeof(void));

            return Block(new ParameterExpression[3]
              {
                indexExp,
                lengthExp,
                variableExp
              }, new Expression[5]
              {
                Assign(indexExp, Constant(0)),
                Assign(lengthExp, ArrayLength(sourceExpression)),
                Assign(variableExp, NewArrayBounds(elementType, lengthExp)),
                Expression.Loop(
                    IfThenElse(
                        GreaterThan(lengthExp, indexExp),
                        Block(
                            Map(ArrayIndex(variableExp, indexExp), ArrayIndex(sourceExpression, indexExp), configuration),
                            AddAssign(indexExp, Constant(1)),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label),
                variableExp
              });
        }

        private static Expression ArrayTo(Type conversionType, IMapConfiguration configuration, Expression sourceExpression)
        {
            Type elementType;

            bool isArray = conversionType.IsArray;

            if (isArray)
            {
                elementType = conversionType.GetElementType();

                conversionType = typeof(List<>).MakeGenericType(elementType);
            }
            else
            {

                foreach (var interfaceType in conversionType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>))
                    {
                        elementType = interfaceType.GetGenericArguments()[0];

                        goto label_core;
                    }
                }

                foreach (var interfaceType in conversionType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType)
                    {
                        var typeDefinition = interfaceType.GetGenericTypeDefinition();

                        if (typeDefinition == typeof(IList<>)
                            || typeDefinition == typeof(IReadOnlyList<>)
                            || typeDefinition == typeof(ICollection<>)
                            || typeDefinition == typeof(IReadOnlyCollection<>)
                            || typeDefinition == typeof(IEnumerable<>))
                        {
                            var constructorInfo = conversionType.GetConstructor(MapExtensions.InstanceFlags, null, new Type[] { interfaceType }, null);

                            if (constructorInfo is null)
                            {
                                continue;
                            }

                            return EnumerableTo(constructorInfo, interfaceType.GetGenericArguments()[0], configuration, sourceExpression);
                        }
                    }
                }

                throw new NotSupportedException($"暂不支持类型({conversionType})的映射!");
            }

label_core:

            var indexExp = Variable(typeof(int));

            var lengthExp = Variable(typeof(int));

            var variableExp = Variable(conversionType);

            LabelTarget break_label = Label(typeof(void));
            LabelTarget continue_label = Label(typeof(void));

            return Block(new ParameterExpression[3]
              {
                indexExp,
                lengthExp,
                variableExp
              }, new Expression[5]
              {
                Assign(indexExp, Constant(0)),
                Assign(lengthExp, ArrayLength(sourceExpression)),
                Assign(variableExp, New(conversionType)),
                Expression.Loop(
                    IfThenElse(
                        GreaterThan(lengthExp, indexExp),
                        Block(
                            MapCall(variableExp, ArrayIndex(sourceExpression, indexExp), elementType, configuration),
                            AddAssign(indexExp, Constant(1)),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label
                ),
                isArray
                ? Call(ToArrayFn.MakeGenericMethod(elementType), variableExp) //? 转数组。
                : (Expression)variableExp
              });
        }

        private static Expression EnumerableGenericTo(Type enumerableGenericType, Type conversionType, IMapConfiguration configuration, Expression sourceExpression)
        {
            Type elementType;

            bool isArray = conversionType.IsArray;

            if (isArray)
            {
                elementType = conversionType.GetElementType();

                conversionType = typeof(List<>).MakeGenericType(elementType);
            }
            else
            {

                foreach (var interfaceType in conversionType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>))
                    {
                        elementType = interfaceType.GetGenericArguments()[0];

                        goto label_core;
                    }
                }

                foreach (var interfaceType in conversionType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType)
                    {
                        var typeDefinition = interfaceType.GetGenericTypeDefinition();

                        if (typeDefinition == typeof(IList<>)
                            || typeDefinition == typeof(IReadOnlyList<>)
                            || typeDefinition == typeof(ICollection<>)
                            || typeDefinition == typeof(IReadOnlyCollection<>)
                            || typeDefinition == typeof(IEnumerable<>))
                        {
                            var constructorInfo = conversionType.GetConstructor(MapExtensions.InstanceFlags, null, new Type[] { interfaceType }, null);

                            if (constructorInfo is null)
                            {
                                continue;
                            }

                            return EnumerableTo(constructorInfo, interfaceType.GetGenericArguments()[0], configuration, sourceExpression);
                        }
                    }
                }

                foreach (var interfaceType in conversionType.GetInterfaces())
                {
                    if (interfaceType == typeof(IList)
                        || interfaceType == typeof(ICollection)
                        || interfaceType == typeof(IEnumerable))
                    {
                        var constructorInfo = conversionType.GetConstructor(MapExtensions.InstanceFlags, null, new Type[] { interfaceType }, null);

                        if (constructorInfo is null)
                        {
                            continue;
                        }

                        return EnumerableTo(constructorInfo, typeof(object), configuration, sourceExpression);
                    }
                }

                throw new NotSupportedException($"暂不支持类型({conversionType})的映射!");
            }

label_core:
            Type enumeratorGenericType = typeof(IEnumerator<>).MakeGenericType(enumerableGenericType.GetGenericArguments());
            MethodInfo getEnumeratorFn = enumerableGenericType.GetMethod("GetEnumerator", Type.EmptyTypes);
            PropertyInfo currentPropertyInfo = enumeratorGenericType.GetProperty("Current");

            LabelTarget break_label = Label(typeof(void));
            LabelTarget continue_label = Label(typeof(void));

            var variableExp = Variable(conversionType);
            var enumeratorExp = Variable(enumeratorGenericType);

            return Block(new ParameterExpression[]
             {
                variableExp,
                enumeratorExp
             }, new Expression[]
             {
                Assign(variableExp, New(conversionType)),
                Assign(enumeratorExp, Call(sourceExpression, getEnumeratorFn)),
                Expression.Loop(
                    IfThenElse(
                        Call(enumeratorExp, MoveNextFn),
                        Block(
                            MapCall(variableExp, Property(enumeratorExp, currentPropertyInfo), elementType, configuration),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label
                ),
                isArray
                ? Call(ToArrayFn.MakeGenericMethod(elementType), variableExp) //? 转数组。
                : (Expression)variableExp
             });
        }

        private static Expression EnumerableTo(Type conversionType, IMapConfiguration configuration, Expression sourceExpression)
        {
            Type elementType;

            bool isArray = conversionType.IsArray;

            if (isArray)
            {
                elementType = conversionType.GetElementType();

                conversionType = typeof(List<>).MakeGenericType(elementType);
            }
            else
            {

                foreach (var interfaceType in conversionType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>))
                    {
                        elementType = interfaceType.GetGenericArguments()[0];

                        goto label_core;
                    }
                }

                foreach (var interfaceType in conversionType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType)
                    {
                        var typeDefinition = interfaceType.GetGenericTypeDefinition();

                        if (typeDefinition == typeof(IList<>)
                            || typeDefinition == typeof(IReadOnlyList<>)
                            || typeDefinition == typeof(ICollection<>)
                            || typeDefinition == typeof(IReadOnlyCollection<>)
                            || typeDefinition == typeof(IEnumerable<>))
                        {
                            var constructorInfo = conversionType.GetConstructor(MapExtensions.InstanceFlags, null, new Type[] { interfaceType }, null);

                            if (constructorInfo is null)
                            {
                                continue;
                            }

                            return EnumerableTo(constructorInfo, interfaceType.GetGenericArguments()[0], configuration, sourceExpression);
                        }
                    }
                }

                foreach (var interfaceType in conversionType.GetInterfaces())
                {
                    if (interfaceType == typeof(IList)
                        || interfaceType == typeof(ICollection)
                        || interfaceType == typeof(IEnumerable))
                    {
                        var constructorInfo = conversionType.GetConstructor(MapExtensions.InstanceFlags, null, new Type[] { interfaceType }, null);

                        if (constructorInfo is null)
                        {
                            continue;
                        }

                        return EnumerableTo(constructorInfo, typeof(object), configuration, sourceExpression);
                    }
                }

                throw new NotSupportedException($"暂不支持类型({conversionType})的映射!");
            }

label_core:
            LabelTarget break_label = Label(typeof(void));
            LabelTarget continue_label = Label(typeof(void));

            var variableExp = Variable(conversionType);
            var enumeratorExp = Variable(typeof(IEnumerator));

            return Block(new ParameterExpression[]
             {
                variableExp,
                enumeratorExp
             }, new Expression[]
             {
                Assign(variableExp, New(conversionType)),
                Assign(enumeratorExp, Call(sourceExpression, GetEnumeratorFn)),
                Expression.Loop(
                    IfThenElse(
                        Call(enumeratorExp, MoveNextFn),
                        Block(
                            MapCall(variableExp, Property(enumeratorExp, CurrentPropertyInfo), elementType, configuration),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label
                ),
                isArray
                ? Call(ToArrayFn.MakeGenericMethod(elementType), variableExp) //? 转数组。
                : (Expression)variableExp
             });
        }

        private static Expression EnumerableTo(ConstructorInfo constructorInfo, Type elementType, IMapConfiguration configuration, Expression sourceExpression)
        {
            Type conversionType = typeof(List<>).MakeGenericType(elementType);

            LabelTarget break_label = Label(typeof(void));
            LabelTarget continue_label = Label(typeof(void));

            var variableExp = Variable(conversionType);
            var enumeratorExp = Variable(typeof(IEnumerator));

            return Block(new ParameterExpression[]
             {
                variableExp,
                enumeratorExp
             }, new Expression[]
             {
                Assign(variableExp, New(conversionType)),
                Assign(enumeratorExp, Call(sourceExpression, GetEnumeratorFn)),
                Expression.Loop(
                    IfThenElse(
                        Call(enumeratorExp, MoveNextFn),
                        Block(
                            MapCall(variableExp, Property(enumeratorExp, CurrentPropertyInfo), elementType, configuration),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label
                ),
                Expression.New(constructorInfo, variableExp)
             });
        }

        private static Expression Map(Expression left, Expression right, IMapConfiguration configuration)
        {
            if (right.Type.IsValueType && !right.Type.IsNullable())
            {
                if (left.Type.IsAssignableFrom(right.Type))
                {
                    return Assign(left, right);
                }

                return Assign(left, configuration.Map(right, left.Type));
            }

            if (configuration.IsDepthMapping == true || !left.Type.IsAssignableFrom(right.Type))
            {
                if (configuration.AllowNullMapping == true)
                {
                    return Assign(left, configuration.Map(right, left.Type));
                }

                return IfThen(NotEqual(right, Constant(null, right.Type)), Assign(left, configuration.Map(right, left.Type)));
            }

            if (configuration.AllowNullMapping == true)
            {
                return Assign(left, right);
            }

            return IfThen(NotEqual(right, Constant(null, right.Type)), Assign(left, right));
        }

        private static Expression MapCall(Expression destExp, Expression right, Type elementType, IMapConfiguration configuration)
        {
            var addFn = typeof(ICollection<>)
                    .MakeGenericType(elementType)
                    .GetMethod("Add", new Type[] { elementType });

            if (right.Type.IsValueType && !right.Type.IsNullable())
            {
                if (elementType.IsAssignableFrom(right.Type))
                {
                    return Call(destExp, addFn, right);
                }

                return Call(destExp, addFn, configuration.Map(right, elementType));
            }

            if (configuration.IsDepthMapping == true || !elementType.IsAssignableFrom(right.Type))
            {
                if (configuration.AllowNullMapping == true)
                {
                    return Call(destExp, addFn, configuration.Map(right, elementType));
                }

                return IfThen(NotEqual(right, Constant(null, right.Type)), Call(destExp, addFn, configuration.Map(right, elementType)));
            }

            if (configuration.AllowNullMapping == true)
            {
                return Call(destExp, addFn, right);
            }

            return IfThen(NotEqual(right, Constant(null, right.Type)), Call(destExp, addFn, right));
        }

        private static Expression New(Type conversionType)
        {
            var constructorInfo = conversionType.GetConstructor(MapExtensions.InstanceFlags, null, Type.EmptyTypes, null);

            if (constructorInfo != null)
            {
                return Expression.New(constructorInfo);
            }

            var ctorWithOptionalArgs = conversionType
                .GetConstructors(MapExtensions.InstanceFlags)
                .FirstOrDefault(c => c.GetParameters().All(p => p.IsOptional));

            if (ctorWithOptionalArgs is null)
            {
                throw new InvalidCastException($"{conversionType}必须包含一个无参构造函数或者只有可选参数的构造函数!");
            }
            //get all optional default values
            var args = ctorWithOptionalArgs.GetParameters().Select(DefaultValue);
            //create the ctor expression
            return Expression.New(ctorWithOptionalArgs, args);
        }

        private static Expression DefaultValue(ParameterInfo x)
        {
            if (x.DefaultValue is null && x.ParameterType.IsValueType)
            {
                return Default(x.ParameterType);
            }

            return Constant(x.DefaultValue, x.ParameterType);
        }
    }
}
