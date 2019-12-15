using System;
using System.Linq.Expressions;

namespace CodeArts
{
    /// <summary>
    /// 单例封装类
    /// </summary>
    /// <typeparam name="T">基类类型</typeparam>
    public static class Singleton<T>
    {
        /// <summary>
        /// 静态构造函数
        /// </summary>
        static Singleton()
        {
            var conversionType = typeof(T);

            var baseType = conversionType.BaseType;

            if (baseType is null || !baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(DesignMode.Singleton<>))
            {
                Instance = (T)Activator.CreateInstance(conversionType, true);
            }
            else
            {
                var propertyExp = Expression.Property(null, conversionType, "Instance");

                var lamdaExp = Expression.Lambda<Func<T>>(propertyExp);

                var invoke = lamdaExp.Compile();

                Instance = invoke.Invoke();
            }
        }

        /// <summary>
        /// 单例
        /// </summary>
        public static T Instance;
    }
}
