using System.Reflection;

namespace CodeArts.Proxies
{
    /// <summary>
    /// 调用者数据信息
    /// </summary>
    public interface IIntercept
    {
        /// <summary>
        /// 调用参数
        /// </summary>
        object[] Arguments { get; }

        /// <summary>
        /// 调用函数
        /// </summary>
        MethodInfo Method { get; }

        /// <summary>
        /// 拦截的对象
        /// </summary>
        object Instance { get; }

        /// <summary>
        /// 返回值
        /// </summary>
        object ReturnValue { get; set; }

        /// <summary>
        /// 执行方法
        /// </summary>
        void Proceed();

        /// <summary>
        /// 获取参数值
        /// </summary>
        /// <param name="index">索引</param>
        /// <returns></returns>
        object GetArgumentValue(int index);

        /// <summary>
        /// 重设参数值
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="value">参数值</param>
        void SetArgumentValue(int index, object value);
    }
}
