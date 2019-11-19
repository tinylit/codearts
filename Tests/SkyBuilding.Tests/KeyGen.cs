using System;
using System.Diagnostics;

namespace SkyBuilding.Tests
{
    /// <summary>
    /// 主键生成器
    /// </summary>
    [DebuggerDisplay("{value}")]
    public class KeyGen
    {
        /// <summary>
        /// 世界时（别名：格林尼治时间）
        /// </summary>
        public readonly static DateTime Universal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        private readonly long value;

        private static long _lastKeyGen;
        /// <summary>
        /// 毫秒计数器
        /// </summary>
        private static long _sequence;
        /// <summary>
        /// 一毫秒内可以产生计数，如果达到该值则等到下一毫妙在进行生成
        /// </summary>
        private static readonly long _sequenceMax = 255L;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host">主机编号</param>
        public KeyGen(byte host = 1) => value = DateGen(host);

        private static long DateGen(byte host)
        {
            long keyGen = TimeGen(host);

            //同一毫妙中生成ID
            if (_lastKeyGen == keyGen)
            {
                _sequence = (_sequence + 1) & _sequenceMax; //用&运算计算该毫秒内产生的计数是否已经到达上限
                if (_sequence == 0)
                {
                    //一毫妙内产生的ID计数已达上限，等待下一毫妙
                    keyGen = Next(host, _lastKeyGen);
                }
            }
            else //不同毫秒生成ID
            {
                _sequence = 0; //计数清0
            }

            while (_lastKeyGen > keyGen)
            {
                keyGen = Next(host, _lastKeyGen);
            }

            _lastKeyGen = keyGen; //把当前时间戳保存为最后生成ID的时间戳

            return keyGen + (_sequence << 8);
        }

        private static long Next(byte host, long keyGen)
        {
            long timestamp = TimeGen(host);

            while (timestamp <= keyGen)
            {
                timestamp = TimeGen(host);
            }

            return timestamp;
        }

        private static long TimeGen(byte host)
        {
            var date = DateTime.Now;

            return date.Month * 100000000000000000L + date.Day * 1000000000000000L + (date.Year - 2000L) * 10000000000000L + date.Hour * 100000000000L + date.Minute * 1000000000L + date.Second * 10000000L + date.Millisecond * 10000L + host;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="keyGen">键</param>
        public KeyGen(long keyGen) => value = keyGen;

        /// <summary>
        /// 键转长整型。
        /// </summary>
        /// <param name="keyGen">生成器</param>
        public static implicit operator long(KeyGen keyGen) => keyGen.value;

        public override string ToString() => value.ToString();
    }
}
