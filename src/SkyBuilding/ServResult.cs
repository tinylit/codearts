using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyBuilding
{
    /// <summary>
    /// 服务调用数据结果
    /// </summary>
    public class ServResult : IResult
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int Code { get; set; }

        private bool? success = null;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success
        {
            get
            {
                if (success.HasValue)
                {
                    return success.Value;
                }
                return Code == StatusCodes.OK;
            }
            set
            {
                success = new bool?(value);
            }
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// Utc
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 类型默认转换
        /// </summary>
        /// <param name="data">数据</param>
        public static implicit operator ServResult(DResult data) => data is null ? null : new ServResult
        {
            Code = data.Code,
            Timestamp = data.Timestamp
        };
    }

    /// <summary>
    /// 服务调用数据结果
    /// </summary>
    /// <typeparam name="T">数据</typeparam>
    public class ServResult<T> : ServResult, IResult<T>, IResult
    {
        /// <summary>
        /// 数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 类型默认转换
        /// </summary>
        /// <param name="data">数据</param>
        public static implicit operator ServResult<T>(DResult<T> data) => data is null ? null : new ServResult<T>
        {
            Code = data.Code,
            Data = data.Data,
            Timestamp = data.Timestamp
        };
    }

    /// <summary>
    /// 服务调用数据结果
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ServResults<T> : ServResult<List<T>>, IResults<T>, IResult<List<T>>, IResult
    {
        /// <summary>
        /// 总条数
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 类型默认转换
        /// </summary>
        /// <param name="data">数据</param>
        public static implicit operator ServResults<T>(DResults<T> data) => data is null ? null : new ServResults<T>
        {
            Code = data.Code,
            Data = data.Data,
            Count = data.Count,
            Timestamp = data.Timestamp
        };
    }
}
