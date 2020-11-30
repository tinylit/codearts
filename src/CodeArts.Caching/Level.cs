using System;
using System.ComponentModel;

namespace CodeArts.Caching
{
    /// <summary> 缓存级别。 </summary>
    [Flags]
    public enum Level
    {
        /// <summary> 一级缓存，本机内存。 </summary>
        [Description("一级缓存")]
        First = 1,

        /// <summary> 二级缓存，分布式。 </summary>
        [Description("二级缓存")]
        Second = 2,

        /// <summary> 三级缓存，预留。 </summary>
        [Description("三级缓存")]
        Third = 4
    }
}
