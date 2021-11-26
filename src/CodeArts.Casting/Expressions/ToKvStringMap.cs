using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Casting.Expressions
{
    using static Expression;
    using kvString = System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>;

    /// <summary>
    /// <see cref="ICollection{T}"/> when T is <seealso cref="KeyValuePair{TKey, TValue}"/> when TKey is <seealso cref="string"/> and TValue is <seealso cref="object"/>.
    /// </summary>
    public class ToKvStringMap : MapExpression
    {
        private readonly static Type keyStringType = typeof(kvString);

        private readonly static MethodInfo addFn = keyStringType.GetMethod("Add");
        private readonly static ConstructorInfo kvCtor = typeof(System.Collections.Generic.KeyValuePair<string, object>).GetConstructor(new Type[2] { typeof(string), typeof(object) });

        private readonly static Type kvType = typeof(IDictionary<string, object>);
        private readonly static MethodInfo kvAddFn = kvType.GetMethod("Add", new Type[2] { typeof(string), typeof(object) });

        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type conversionType) => keyStringType.IsAssignableFrom(conversionType);

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
            var destExp = New(conversionType);

            var variableExp = Variable(conversionType);

            bool flag = kvType.IsAssignableFrom(conversionType);

            var expressions = new List<Expression> { Assign(variableExp, destExp) };

            if (configuration.Kind == PatternKind.Property || configuration.Kind == PatternKind.All)
            {
                foreach (var propertyInfo in sourceType.GetProperties())
                {
                    if (!propertyInfo.CanRead)
                    {
                        continue;
                    }

                    expressions.Add(Map(variableExp, Constant(propertyInfo.Name), Property(sourceExpression, propertyInfo), flag, configuration));
                }
            }

            if (configuration.Kind == PatternKind.Field || configuration.Kind == PatternKind.All)
            {
                foreach (var fieldInfo in sourceType.GetFields())
                {
                    if (fieldInfo.IsStatic || fieldInfo.IsInitOnly)
                    {
                        continue;
                    }

                    expressions.Add(Map(variableExp, Constant(fieldInfo.Name), Field(sourceExpression, fieldInfo), flag, configuration));
                }
            }

            expressions.Add(variableExp); //? 结果返回。

            return Block(conversionType, new ParameterExpression[1] { variableExp }, expressions);

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

        private static Expression Map(Expression destExp, Expression key, Expression value, bool flag, IMapConfiguration configuration)
        {
            var valueType = value.Type;

            if (valueType.IsValueType)
            {
                var bodyExp = flag
                    ? Call(destExp, kvAddFn, new Expression[2] { key, Convert(value, typeof(object)) })
                    : Call(destExp, addFn, Expression.New(kvCtor, new Expression[2] { key, Convert(value, typeof(object)) }));

                if (configuration.AllowNullMapping == true)
                {
                    return bodyExp;
                }

                if (valueType.IsNullable())
                {
                    return IfThen(NotEqual(value, Constant(null, valueType)), bodyExp);
                }

                return bodyExp;
            }
            else
            {
                var bodyExp = flag
                    ? Call(destExp, kvAddFn, new Expression[2] { key, value })
                    : Call(destExp, addFn, Expression.New(kvCtor, new Expression[2] { key, value }));

                if (configuration.AllowNullMapping == true)
                {
                    return bodyExp;
                }

                return IfThen(NotEqual(value, Constant(null, valueType)), bodyExp);
            }
        }
    }
}
