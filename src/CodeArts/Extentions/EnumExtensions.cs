using CodeArts;
using System.ComponentModel;
using System.Linq;

namespace System
{
    /// <summary>
    /// 枚举扩展
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// 获取枚举描述
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="enum">枚举值</param>
        /// <returns></returns>
        public static string GetText<TEnum>(this TEnum @enum) where TEnum : struct
        {
            var type = typeof(TEnum);

            if (!type.IsEnum)
                throw new ArgumentException("参数类型不是枚举!");

            var typeStore = RuntimeTypeCache.Instance.GetCache<TEnum>();

            string enumStr = @enum.ToString();

            if (typeStore.IsDefined<FlagsAttribute>())
            {
                var values = enumStr.Split(',').Select(x => x.Trim());

                if (!values.All(x => typeStore.FieldStores.Any(y => string.Equals(y.Name, x, StringComparison.InvariantCultureIgnoreCase))))
                    return "枚举错误";

                return string.Join("|", typeStore.FieldStores
                    .Where(x => values.Any(y => string.Equals(y, x.Name, StringComparison.InvariantCultureIgnoreCase)))
                    .Select(x =>
                    {
                        var desc2 = x.GetCustomAttribute<DescriptionAttribute>();

                        return desc2 is null ? x.Name : desc2.Description;
                    }));
            }

            var field = typeStore.FieldStores.FirstOrDefault(x => x.Name == enumStr);

            if (field is null)
                return "枚举错误";

            var desc = field.GetCustomAttribute<DescriptionAttribute>();

            return desc is null ? field.Name : desc.Description;
        }
    }
}
