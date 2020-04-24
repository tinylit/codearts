namespace CodeArts.Emit
{
    /// <summary>
    /// 名称供应器。
    /// </summary>
    public interface INamingProvider
    {
        /// <summary>
        /// 获取唯一的名称。
        /// </summary>
        /// <param name="displayName">显示名称。</param>
        /// <returns></returns>
        string GetUniqueName(string displayName);
    }
}
