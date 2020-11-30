namespace CodeArts
{
    /// <summary>
    /// 雪花算法创建器。
    /// </summary>
    public class SnowflakeFactory : IKeyGenFactory
    {
        private readonly int workerId = 0;
        private readonly int datacenterId = 0;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public SnowflakeFactory() { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="workerId">机器ID。</param>
        /// <param name="datacenterId">机房ID。</param>
        public SnowflakeFactory(int workerId, int datacenterId)
        {
            this.workerId = workerId;
            this.datacenterId = datacenterId;
        }

        /// <summary>
        /// 创建。
        /// </summary>
        /// <returns></returns>
        public IKeyGen Create() => new SnowflakeKeyGen(workerId, datacenterId);
    }
}
