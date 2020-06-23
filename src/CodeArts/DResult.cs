using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace CodeArts
{
    /// <summary>
    /// 数据结果
    /// </summary>
    [XmlRoot("xml")]
    public class DResult : IResult
    {
        /// <summary>
        /// 错误信息实体
        /// </summary>
        [XmlRoot("xml")]
        private class ErrorResult : DResult, IResult
        {
            /// <summary>
            /// 用作Xml解析
            /// </summary>
            private ErrorResult() { }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="errorMsg">错误消息</param>
            /// <param name="statusCode">错误编码</param>
            public ErrorResult(string errorMsg, int statusCode)
            {
                Msg = errorMsg;
                Code = statusCode;
            }

            /// <summary>
            /// 错误信息
            /// </summary>
            [XmlElement("msg")]
            public string Msg { get; private set; }
        }

        /// <summary>
        /// 错误信息实体
        /// </summary>
        [XmlRoot("xml")]
        private class ErrorResult<T> : DResult<T>, IResult<T>, IResult
        {
            /// <summary>
            /// 用作Xml解析
            /// </summary>
            private ErrorResult() : base(default) { }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="errorMsg">错误消息</param>
            /// <param name="statusCode">错误编码</param>
            public ErrorResult(string errorMsg, int statusCode) : this()
            {
                Msg = errorMsg;
                Code = statusCode;
            }

            /// <summary>
            /// 错误信息
            /// </summary>
            [XmlElement("msg")]
            public string Msg { get; private set; }
        }

        /// <summary>
        /// 错误信息实体
        /// </summary>
        [XmlRoot("xml")]
        private class ErrorResults<T> : DResults<T>, IResults<T>, IResult<List<T>>, IResult
        {
            /// <summary>
            /// 用作Xml解析
            /// </summary>
            private ErrorResults() : base(default) { }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="errorMsg">错误消息</param>
            /// <param name="statusCode">错误编码</param>
            public ErrorResults(string errorMsg, int statusCode) : this()
            {
                Msg = errorMsg;
                Code = statusCode;
            }

            /// <summary>
            /// 错误信息
            /// </summary>
            [XmlElement("msg")]
            public string Msg { get; private set; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public DResult() { }

        /// <summary>
        /// 状态码
        /// </summary>
        [XmlElement("code")]
        public int Code { get; private set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        [XmlIgnore]
        public bool Success => Code == StatusCodes.OK;

        /// <summary>
        /// Utc
        /// </summary>
        [XmlElement("timestamp")]
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// 成功
        /// </summary>
        public static DResult Ok() => new DResult();

        /// <summary>
        /// 成功
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns></returns>IRequestable
        public static DResult<T> Ok<T>(T data) => new DResult<T>(data);

        /// <summary>
        /// 成功
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns></returns>
        public static DResults<T> Ok<T>(PagedList<T> data) => new DResults<T>(data);

        /// <summary>
        /// 成功
        /// </summary>
        /// <param name="total">总数</param>
        /// <param name="data">数据</param>
        /// <returns></returns>
        public static DResults<T> Ok<T>(int total, List<T> data) => new DResults<T>(total, data);

        /// <summary>
        /// 错误信息
        /// </summary>
        /// <param name="errorMsg">错误信息</param>
        /// <param name="statusCode">状态码</param>
        /// <returns></returns>
        public static DResult Error(string errorMsg, int statusCode = StatusCodes.Error) => new ErrorResult(errorMsg, statusCode);

        /// <summary>
        /// 错误信息
        /// </summary>
        /// <param name="errorMsg">错误信息</param>
        /// <param name="statusCode">状态码</param>
        /// <returns></returns>
        public static DResult<T> Error<T>(string errorMsg, int statusCode = StatusCodes.Error) => new ErrorResult<T>(errorMsg, statusCode);

        /// <summary>
        /// 错误信息
        /// </summary>
        /// <param name="errorMsg">错误信息</param>
        /// <param name="statusCode">状态码</param>
        /// <returns></returns>
        public static DResults<T> Errors<T>(string errorMsg, int statusCode = StatusCodes.Error) => new ErrorResults<T>(errorMsg, statusCode);

        /// <summary>
        /// 服务结果实体转数据结果实体。
        /// </summary>
        /// <param name="data">服务结果</param>
        /// <returns></returns>
        public static DResult ConvertFrom(ServResult data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var result = data.Success ? Ok() : Error(data.Msg, data.Code);

            result.Timestamp = data.Timestamp;

            return result;
        }

        /// <summary>
        /// 服务结果实体转数据结果实体。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="data">服务结果</param>
        /// <returns></returns>
        public static DResult<T> ConvertFrom<T>(ServResult<T> data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var result = data.Success ? Ok(data.Data) : Error<T>(data.Msg, data.Code);

            result.Timestamp = data.Timestamp;

            return result;
        }

        /// <summary>
        /// 服务结果实体转数据结果实体。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="data">服务结果</param>
        /// <returns></returns>
        public static DResults<T> ConvertFrom<T>(ServResults<T> data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var result = data.Success ? Ok(data.Count, data.Data) : Errors<T>(data.Msg, data.Code);

            result.Timestamp = data.Timestamp;

            return result;
        }

        /// <summary>
        /// 类型默认转换
        /// </summary>
        /// <param name="data">数据</param>
        public static implicit operator DResult(ServResult data) => ConvertFrom(data);
    }

    /// <summary>
    /// 数据结果
    /// </summary>
    /// <typeparam name="T">数据</typeparam>
    [XmlRoot("xml")]
    public class DResult<T> : DResult, IResult<T>, IResult
    {
        /// <summary>
        /// 用于Xml解析
        /// </summary>
        private DResult() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="data">数据</param>
        public DResult(T data) : base() => Data = data;

        /// <summary>
        /// 数据
        /// </summary>
        [XmlElement("data")]
        public T Data { get; private set; }

        /// <summary>
        /// 类型默认转换
        /// </summary>
        /// <param name="data">数据</param>
        public static implicit operator DResult<T>(T data) => Ok(data);

        /// <summary>
        /// 类型默认转换
        /// </summary>
        /// <param name="data">数据</param>
        public static implicit operator DResult<T>(ServResult<T> data) => ConvertFrom(data);
    }

    /// <summary>
    /// 数据结果
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    [XmlRoot("xml")]
    public class DResults<T> : DResult<List<T>>, IResults<T>, IResult<List<T>>, IResult
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="data">分页的数据</param>
        public DResults(PagedList<T> data) : base(data?.ToList()) => Count = data?.Count ?? 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="count">总条数</param>
        /// <param name="data">数据</param>
        public DResults(int count, List<T> data) : base(data) => Count = count;

        /// <summary>
        /// 总条数
        /// </summary>
        [XmlElement("count")]
        public int Count { get; private set; }

        /// <summary>
        /// 类型默认转换
        /// </summary>
        /// <param name="list">数据</param>
        public static implicit operator DResults<T>(List<T> list) => Ok(list?.Count ?? 0, list);

        /// <summary>
        /// 类型默认转换
        /// </summary>
        /// <param name="list">数据</param>
        public static implicit operator DResults<T>(PagedList<T> list) => Ok(list);

        /// <summary>
        /// 类型默认转换
        /// </summary>
        /// <param name="data">数据</param>
        public static implicit operator DResults<T>(ServResults<T> data) => ConvertFrom(data);
    }
}
