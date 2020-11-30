namespace CodeArts
{
    /// <summary>
    /// 配置变更监听能力。
    /// </summary>
    public interface IConfigable<TSelf> where TSelf : class, IConfigable<TSelf>
    {
        /// <summary>
        /// 监听到变更后的新数据。
        /// </summary>
        /// <param name="changedValue">变更后的数据。</param>
        void SaveChanges(TSelf changedValue);
    }
}
