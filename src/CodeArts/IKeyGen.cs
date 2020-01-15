namespace CodeArts
{
    /// <summary>
    /// 主键生成器接口
    /// </summary>
    public interface IKeyGen
    {
        /// <summary>
        /// 生成新主键
        /// </summary>
        /// <returns></returns>
        long Id();
        /// <summary>
        /// 创建主键
        /// </summary>
        /// <param name="key">主键值</param>
        /// <returns></returns>
        Key Create(long key);
    }
}
