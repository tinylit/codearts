using System.Reflection;

namespace CodeArts.Proxies
{
    /// <summary>
    /// 拦截信息
    /// </summary>
    public class Intercept : IIntercept
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="instance">拦截的实例</param>
        /// <param name="method">拦截函数</param>
        /// <param name="arguments">函数参数</param>
        public Intercept(object instance, MethodInfo method, object[] arguments)
        {
            Instance = instance;
            Method = method;
            Arguments = arguments;
        }

        /// <summary>
        /// 函数参数。
        /// </summary>
        public object[] Arguments { get; }
        /// <summary>
        /// 函数。
        /// </summary>
        public MethodInfo Method { get; }
        /// <summary>
        /// 拦截实例
        /// </summary>
        public object Instance { get; }
        /// <summary>
        /// 返回值。
        /// </summary>
        public object ReturnValue { get; set; }
        /// <summary>
        /// 获取指定索引参数值。
        /// </summary>
        /// <param name="index">索引(小于零时，会加上当前参数数组长度作为新的索引)</param>
        /// <returns></returns>
        public object GetArgumentValue(int index) => Arguments[index > -1 ? index : Arguments.Length + index];
        /// <summary>
        /// 继续执行。
        /// </summary>
        public void Proceed() => ReturnValue = Method.Invoke(Instance, Arguments);

        /// <summary>
        /// 设置指定索引参数值。
        /// </summary>
        /// <param name="index">索引（小于零时，会加上当前参数数组长度作为新的索引）</param>
        /// <param name="value">值</param>
        public void SetArgumentValue(int index, object value) => Arguments[index > -1 ? index : Arguments.Length + index] = value;
    }
}
