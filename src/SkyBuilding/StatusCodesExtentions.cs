using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Reflection;

namespace SkyBuilding
{
    /// <summary>
    /// 状态码扩展
    /// </summary>
    public static class StatusCodesExtentions
    {
        /// <summary>
        /// 错误码基类
        /// </summary>
        private static readonly Type StatusCodeType = typeof(StatusCodes);

        //各错误码类型及其错误码描述存储
        private static readonly ConcurrentDictionary<Type, Dictionary<int, string>> StatusCodesCache =
            new ConcurrentDictionary<Type, Dictionary<int, string>>();

        /// <summary> 获取状态码的信息 </summary>
        /// <param name="statusCode">状态码</param>
        /// <returns></returns>
        public static string Message(this int statusCode) => statusCode.Message<StatusCodes>();

        /// <summary> 获取状态码的信息 </summary>
        /// <param name="statusCode">状态码</param>
        /// <returns></returns>
        public static string Message<T>(this int statusCode) where T : StatusCodes
            => Codes<T>().TryGetValue(statusCode, out var message) ? message : StatusCodes.SystemError.Message<StatusCodes>();


        /// <summary> 错误编码对应DResult </summary>
        /// <param name="statusCode">状态码</param>
        /// <returns></returns>
        public static DResult CodeResult(this int statusCode) => statusCode.CodeResult<StatusCodes>();

        /// <summary> 错误编码对应DResult </summary>
        /// <typeparam name="T">状态码类型</typeparam>
        /// <param name="statusCode">状态码</param>
        /// <returns></returns>
        public static DResult CodeResult<T>(this int statusCode) where T : StatusCodes
            => StatusCodes.OK == statusCode ? DResult.Ok() : DResult.Error(statusCode.Message<T>(), statusCode);

        /// <summary> 获取状态码的信息 </summary>
        /// <param name="statusCode">状态码</param>
        /// <returns></returns>
        public static DResult CodeResult(this HttpStatusCode statusCode) => ((int)statusCode).CodeResult();

        /// <summary> 获取状态码的信息 </summary>
        /// <param name="statusCode">状态码</param>
        /// <returns></returns>
        public static DResult CodeResult<T>(this HttpStatusCode statusCode) where T : StatusCodes
            => ((int)statusCode).CodeResult<T>();

        /// <summary> 获取指定类型的错误码 </summary>
        /// <typeparam name="T">错误码类型</typeparam>
        /// <returns></returns>
#if NET40
        public static IDictionary<int, string> Codes<T>() where T : StatusCodes
#else
        public static IReadOnlyDictionary<int, string> Codes<T>() where T : StatusCodes
#endif
            => StatusCodesCache.GetOrAdd(typeof(T), type =>
            {
                //遍历获取当前类型错误码信息
                var codes = new Dictionary<int, string>();

                //递归调用获取父级类型的错误码信息
                do
                {
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
#if NET40
                        var attr = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
#else
                        var attr = field.GetCustomAttribute<DescriptionAttribute>();
#endif

                        var message = attr is null ? field.Name : attr.Description ?? field.Name;

                        codes.Add((int)field.GetRawConstantValue(), message);
                    }

                } while (StatusCodeType.IsAssignableFrom(type = type.BaseType));

                return codes;
            });
    }
}
