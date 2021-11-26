using CodeArts.Casting.Expressions;
using System.Collections.Generic;

namespace CodeArts.Casting
{
    /// <summary>
    /// 转换映射。
    /// </summary>
    public class CastingMapper : Mapper<CastingMapper>, IMapper
    {
        private readonly static List<IMapExpression> DefaultMaps = new List<IMapExpression>
        {
           new EnumerableMap(),
           new ToKvStringMap(),
           new ConvertMap(),
           new ParseStringMap(),
           new EnumToEnumMap(),
           new UnderlyingTypeEnumMap(),
           new StringToEnumMap(),
           new KeyValueMap(),
           new ConstructorMap(),
           new ConversionOperatorMap("op_Implicit"),
           new ConversionOperatorMap("op_Explicit"),
           new FromKvStringMap(),
           new ToKvStringMap(),
           new AutomaticMap()
        };

        /// <summary>
        /// 构造函数。
        /// </summary>
        public CastingMapper() : base(DefaultMaps)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public CastingMapper(IProfileConfiguration profile) : base(profile, DefaultMaps)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public CastingMapper(IEnumerable<MapExpression> maps) : base(maps)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        public CastingMapper(IProfileConfiguration profile, IEnumerable<MapExpression> maps) : base(profile, maps)
        {
        }

        /// <summary>
        /// 对象克隆。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public T Copy<T>(T obj, T def = default) => Map(obj, def);
    }
}
