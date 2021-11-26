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
    using kvString = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>;

    /// <summary>
    /// <see cref="IEnumerable{T}"/> when T is <seealso cref="KeyValuePair{TKey, TValue}"/> when TKey is <seealso cref="string"/> and TValue is <seealso cref="object"/>.
    /// </summary>
    public class FromKvStringMap : MapExpression
    {
        private readonly static Type keyStringType = typeof(kvString);
        private readonly static Type keyValueStringType = typeof(System.Collections.Generic.KeyValuePair<string, object>);

        private readonly static Type keyValueEnumeratorStringType = typeof(IEnumerator<System.Collections.Generic.KeyValuePair<string, object>>);

        private readonly static MethodInfo GetEnumeratorFn = keyStringType.GetMethod("GetEnumerator", Type.EmptyTypes);
        private readonly static PropertyInfo CurrentPropertyInfo = keyValueEnumeratorStringType.GetProperty("Current");
        private readonly static MethodInfo MoveNextFn = typeof(IEnumerator).GetMethod("MoveNext", Type.EmptyTypes);
        private readonly static PropertyInfo KeyPropertyInfo = keyValueStringType.GetProperty("Key");
        private readonly static PropertyInfo ValuePropertyInfo = keyValueStringType.GetProperty("Value");
        private readonly static MethodInfo ToPascalCaseFn = typeof(StringExtentions).GetMethod("ToPascalCase", MapExtensions.StaticFlags, null, new Type[1] { typeof(string) }, null);

        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type conversionType) => keyStringType.IsAssignableFrom(sourceType);

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
            LabelTarget break_label = Label(typeof(void));
            LabelTarget continue_label = Label(typeof(void));

            var variableExp = Variable(conversionType);
            var keyValueExp = Variable(typeof(KeyValuePair<string, object>));
            var enumeratorExp = Variable(typeof(IEnumerator<KeyValuePair<string, object>>));

            return Block(new ParameterExpression[]
             {
                variableExp,
                keyValueExp,
                enumeratorExp
             }, new Expression[]
             {
                Assign(variableExp, New(conversionType)),
                Assign(enumeratorExp, Call(sourceExpression, GetEnumeratorFn)),
                Expression.Loop(
                    IfThenElse(
                        Call(enumeratorExp, MoveNextFn),
                        Block(
                            Assign(keyValueExp, Property(enumeratorExp, CurrentPropertyInfo)),
                            SwitchKeyValue(Property(keyValueExp, KeyPropertyInfo), Property(keyValueExp, ValuePropertyInfo), variableExp, conversionType, configuration),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label),
                variableExp
             });
        }

        private static Expression SwitchKeyValue(Expression keyExp, Expression valueExp, Expression destExp, Type conversionType, IMapConfiguration configuration)
        {
            List<SwitchCase> switchCases = new List<SwitchCase>();

            if (configuration.Kind == PatternKind.Property || configuration.Kind == PatternKind.All)
            {
                foreach (var propertyInfo in conversionType.GetProperties())
                {
                    if (!propertyInfo.CanWrite)
                    {
                        continue;
                    }

                    switchCases.Add(SwitchCase(Map(Property(destExp, propertyInfo), valueExp, configuration)
                        , Constant(propertyInfo.Name.ToPascalCase())));
                }
            }

            if (configuration.Kind == PatternKind.Field || configuration.Kind == PatternKind.All)
            {
                foreach (var fieldInfo in conversionType.GetFields())
                {
                    if (fieldInfo.IsStatic || fieldInfo.IsInitOnly)
                    {
                        continue;
                    }

                    switchCases.Add(SwitchCase(Map(Field(destExp, fieldInfo), valueExp, configuration)
                        , Constant(fieldInfo.Name.ToPascalCase())));
                }
            }

            return Switch(Call(ToPascalCaseFn, keyExp), null, null, switchCases);
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
    }
}
