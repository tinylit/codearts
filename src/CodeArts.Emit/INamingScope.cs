namespace CodeArts.Emit
{
    /// <summary>
    /// 名称供应器。
    /// </summary>
    public interface INamingScope
    {
        /// <summary>
        /// 获取范围内唯一的名称。
        /// </summary>
        /// <param name="displayName">显示名称。</param>
        /// <returns></returns>
        string GetUniqueName(string displayName);

        /// <summary>
        /// 子命名范围。
        /// </summary>
        /// <returns></returns>
        INamingScope BeginScope();
    }
}
