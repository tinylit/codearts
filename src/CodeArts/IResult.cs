using System;
using System.Collections.Generic;

namespace CodeArts
{
    /// <summary>
    /// 数据结果接口。
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// 状态码。
        /// </summary>
        int Code { get; }

        /// <summary>
        /// 是否成功。
        /// </summary>
        bool Success { get; }

        /// <summary>
        /// 时间戳。
        /// </summary>
        DateTime Timestamp { get; }
    }

    /// <summary>
    /// 数据结果接口。
    /// </summary>
    public interface IResult<T> : IResult
    {
        /// <summary>
        /// 数据。
        /// </summary>
        T Data { get; }
    }

    /// <summary>
    /// 分页数据结果接口。
    /// </summary>
    public interface IResults<T> : IResult<List<T>>
    {
        /// <summary>
        /// 总条数。
        /// </summary>
        int Count { get; }
    }
}
