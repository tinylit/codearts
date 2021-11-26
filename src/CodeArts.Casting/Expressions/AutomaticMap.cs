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

    /// <summary>
    /// 可赋值。
    /// </summary>
    public class AutomaticMap : MapExpression
    {
        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type conversionType)
        {
            if (sourceType.IsSimpleType() || conversionType.IsSimpleType())
            {
                return false;
            }

            return !conversionType.IsInterface && !conversionType.IsAbstract;
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
            if (sourceType.IsValueType && sourceType == conversionType)
            {
                return sourceExpression;
            }

            if (conversionType.IsAssignableFrom(sourceType) && typeof(ICloneable).IsAssignableFrom(sourceType))
            {
                return Convert(Call(sourceExpression, MapExtensions.Clone), conversionType);
            }

            var destExp = New(sourceType, conversionType, sourceExpression);

            var variableExp = Variable(conversionType);

            var expressions = new List<Expression>
            {
                Assign(variableExp, destExp)
            };

            if (configuration.Kind == PatternKind.Property || configuration.Kind == PatternKind.All)
            {
                sourceType.GetProperties()
                    .JoinEach(conversionType.GetProperties(), x => x.Name, y => y.Name, (x, y) =>
                    {
                        if (y.CanWrite && x.CanRead)
                        {
                            expressions.Add(Map(Property(variableExp, y), Property(sourceExpression, x), configuration));
                        }
                    });
            }

            if (configuration.Kind == PatternKind.Field || configuration.Kind == PatternKind.All)
            {
                sourceType.GetFields()
                    .JoinEach(conversionType.GetFields(), x => x.Name, y => y.Name, (x, y) =>
                    {
                        if (x.IsInitOnly || x.IsStatic || y.IsInitOnly || y.IsStatic)
                        {
                            return;
                        }

                        expressions.Add(Map(Field(variableExp, y), Field(sourceExpression, x), configuration));
                    });
            }

            expressions.Add(variableExp);

            return Block(conversionType, new ParameterExpression[1] { variableExp }, expressions);
        }

        private static Expression New(Type sourceType, Type conversionType, Expression sourceExpression)
        {
            var constructorInfos = conversionType.GetConstructors(MapExtensions.InstanceFlags);

            //? 含无参构造函数。
            foreach (var constructorInfo in constructorInfos)
            {
                var parameterInfos = constructorInfo.GetParameters();

                if (parameterInfos.Length == 0)
                {
                    return Expression.New(constructorInfo);
                }
            }

            //? 所有参数可选。
            foreach (var constructorInfo in constructorInfos)
            {
                var parameterInfos = constructorInfo.GetParameters();

                if (parameterInfos.All(x => x.IsOptional))
                {
                    return Expression.New(constructorInfo, parameterInfos.Select(DefaultValue));
                }
            }

            var propertyInfos = sourceType.GetProperties(MapExtensions.InstanceFlags);

            foreach (var constructorInfo in constructorInfos) //? 匿名对象。
            {
                var parameterInfos = constructorInfo.GetParameters();

                if (parameterInfos.All(x => x.IsOptional || propertyInfos.Any(y => y.CanRead && y.Name == x.Name)))
                {
                    return Expression.New(constructorInfo, parameterInfos.Select(x =>
                    {
                        if (x.IsOptional)
                        {
                            return DefaultValue(x);
                        }

                        return Property(sourceExpression, propertyInfos.First(y => y.Name == x.Name));
                    }));
                }
            }

            throw new InvalidCastException($"未找到({sourceType})=>({conversionType})的构造函数!");
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
