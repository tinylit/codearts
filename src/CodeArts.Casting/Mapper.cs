using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Casting
{
    /// <summary>
    /// 配置。
    /// </summary>
    public abstract class Mapper<TMapper> : ProfileExpression<TMapper, IProfileMap>, IProfileMap, IMapConfiguration, IProfileExpression, IProfile, IDisposable where TMapper : Mapper<TMapper>
    {
        /// <summary>
        /// 匹配模式。
        /// </summary>
        public PatternKind Kind { get; } = PatternKind.Property;

        /// <summary>
        /// 深度映射。
        /// </summary>
        public bool? IsDepthMapping { get; } = true;

        /// <summary>
        /// 允许空目标值。
        /// </summary>
        public bool? AllowNullDestinationValues { get; } = true;

        /// <summary>
        /// 允许空值传播映射。
        /// </summary>
        public bool? AllowNullMapping { get; } = false;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="maps">映射表达式。</param>
        protected Mapper(IEnumerable<IMapExpression> maps) : base(maps)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="profile">配置。</param>
        /// <param name="maps">映射表达式。</param>
        protected Mapper(IProfileConfiguration profile, IEnumerable<IMapExpression> maps) : base(maps)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            Kind = profile.Kind;
            IsDepthMapping = profile.IsDepthMapping ?? true;
            AllowNullMapping = profile.AllowNullMapping ?? false;
        }

        /// <summary>
        /// 创建表达式。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        protected override Func<object, TResult> Create<TResult>(Type sourceType, Type conversionType)
        {
            foreach (MapExpression item in Maps.OfType<MapExpression>())
            {
                if (item.IsMatch(sourceType, conversionType))
                {
                    return item.ToSolve<TResult>(sourceType, conversionType, this);
                }
            }

            return base.Create<TResult>(sourceType, conversionType);
        }

        Expression IMapConfiguration.Map(Expression sourceExpression, Type conversionType, Expression def)
        {
            if (sourceExpression is null)
            {
                throw new ArgumentNullException(nameof(sourceExpression));
            }

            if (def is null)
            {
                def = Expression.Default(conversionType);
            }
            else if (!conversionType.IsAssignableFrom(def.Type))
            {
                throw new InvalidCastException($"不能将类型({def.Type})赋值到({conversionType})!");
            }

            var mapFn = MapExtensions.MapGeneric.MakeGenericMethod(conversionType);

            return Expression.Call(Expression.Constant(this), mapFn, new Expression[2] { Expression.Convert(sourceExpression, typeof(object)), def });
        }
    }
}
