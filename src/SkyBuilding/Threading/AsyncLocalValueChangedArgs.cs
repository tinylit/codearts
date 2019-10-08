namespace System.Threading
{
#if NET40 || NET45 || NET451 || NET452
    /// <summary>
    /// 异步数据改变参数
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public struct AsyncLocalValueChangedArgs<T>
    {
        /// <summary>
        /// 改变前的数据
        /// </summary>
        public T PreviousValue { get; }
        /// <summary>
        /// 当前数据
        /// </summary>
        public T CurrentValue { get; }
        /// <summary>
        /// 线程上下文是否发生改变
        /// </summary>
        public bool ThreadContextChanged { get; }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="previousValue">改变前的数据</param>
        /// <param name="currentValue">当前数据</param>
        /// <param name="contextChanged">上下文是否改变</param>
        internal AsyncLocalValueChangedArgs(T previousValue, T currentValue, bool contextChanged)
        {
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            ThreadContextChanged = contextChanged;
        }
    }

#endif
}
