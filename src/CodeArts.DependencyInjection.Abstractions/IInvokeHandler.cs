namespace CodeArts.DependencyInjection.Abstractions
{
    /// <summary>
    /// 调用处理程序。
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public interface IInvokeHandler<TContext>
    {
        void Invoke();
    }
}
