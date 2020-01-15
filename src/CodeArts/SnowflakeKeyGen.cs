using System;

namespace CodeArts
{
    /// <summary>
    /// 雪花算法
    /// </summary>
    public class SnowflakeKeyGen : IKeyGen
    {
        private class SnowflakeKey : Key
        {
            public SnowflakeKey(long value) : base(value)
            {
            }

            public override DateTime ToUniversalTime() => UtcBase.AddMilliseconds(Value >> timestampLeftShift);
        }

        private readonly static DateTime UtcBase = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private readonly long workerId = 0L; // 这个就是代表了机器id
        private readonly long datacenterId = 0L; // 这个就是代表了机房id

        private static long sequence = 0L; // 代表当前毫秒内已经生成了多少个主键

        /// <summary>
        /// 构造函数（机房：0，工作机号：0）
        /// </summary>
        public SnowflakeKeyGen() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="workerId">机器ID</param>
        /// <param name="datacenterId">机房ID</param>
        public SnowflakeKeyGen(int workerId, int datacenterId)
        {
            // sanity check for workerId
            // 这儿不就检查了一下，要求就是你传递进来的机房id和机器id不能超过32，不能小于0
            if (workerId > maxWorkerId || workerId < 0)
            {
                throw new ArgumentException(string.Format("worker Id can't be greater than {0} or less than 0", maxWorkerId));
            }

            if (datacenterId > maxDatacenterId || datacenterId < 0)
            {
                throw new ArgumentException(string.Format("datacenter Id can't be greater than {0} or less than 0", maxDatacenterId));
            }
            this.workerId = workerId;
            this.datacenterId = datacenterId;
        }

        private readonly static long _timestamp = 0L;
        private readonly static int workerIdBits = 5;
        private readonly static int datacenterIdBits = 5;

        // 这个是二进制运算，就是5 bit最多只能有31个数字，也就是说机器id最多只能是32以内
        private readonly static int maxWorkerId = -1 ^ (-1 << workerIdBits);
        // 这个是一个意思，就是5 bit最多只能有31个数字，机房id最多只能是32以内
        private readonly static int maxDatacenterId = -1 ^ (-1 << datacenterIdBits);
        private readonly static int sequenceBits = 12;
        private readonly static int workerIdShift = sequenceBits;
        private readonly static int datacenterIdShift = sequenceBits + workerIdBits;
        private readonly static int timestampLeftShift = sequenceBits + workerIdBits + datacenterIdBits;
        private readonly static long sequenceMask = -1L ^ (-1L << sequenceBits);
        private long lastTimestamp = -1L;

        /// <summary>
        /// 新ID
        /// </summary>
        /// <returns></returns>
        public long Id()
        {
            long timestamp = TimeGen();
            if (timestamp < lastTimestamp)
            {
                throw new Exception(string.Format("Clock moved backwards. Refusing to generate id for {0} milliseconds", lastTimestamp - timestamp));
            }

            // 下面是说假设在同一个毫秒内，又发送了一个请求生成一个id
            // 这个时候就得把seqence序号给递增1，最多就是4096
            if (lastTimestamp == timestamp)
            {
                // 这个意思是说一个毫秒内最多只能有4096个数字，无论你传递多少进来，
                //这个位运算保证始终就是在4096这个范围内，避免你自己传递个sequence超过了4096这个范围
                sequence = (sequence + 1L) & sequenceMask;
                if (sequence == 0L)
                {
                    timestamp = NextGen(lastTimestamp);
                }
            }
            else
            {
                sequence = 0;
            }

            lastTimestamp = timestamp;

            return ((timestamp - _timestamp) << timestampLeftShift)
                    | (datacenterId << datacenterIdShift)
                    | (workerId << workerIdShift)
                    | sequence;
        }

        /// <summary>
        /// 生成指定键值的键
        /// </summary>
        /// <param name="id">键值</param>
        /// <returns></returns>
        public Key Create(long id) => new SnowflakeKey(id);

        /// <summary>
        /// 当前时间戳
        /// </summary>
        /// <returns></returns>
        private static long TimeGen() => (long)(DateTime.UtcNow - UtcBase).TotalMilliseconds;

        /// <summary>
        /// 获取下一毫秒时间戳
        /// </summary>
        /// <param name="lastTimestamp"></param>
        /// <returns></returns>
        private static long NextGen(long lastTimestamp)
        {
            long timestamp = TimeGen();
            while (timestamp <= lastTimestamp)
            {
                timestamp = TimeGen();
            }
            return timestamp;
        }
    }
}
