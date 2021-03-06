﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace CodeArts
{
    /// <summary>
    /// 服务调用数据结果。
    /// </summary>
    [XmlRoot("xml")]
    public class ServResult : IResult
    {
        /// <summary>
        /// 状态码。
        /// </summary>
        [XmlElement("code")]
        public int Code { get; set; }

        private bool? success = null;

        /// <summary>
        /// 是否成功。
        /// </summary>
        [XmlIgnore]
        public bool Success
        {
            get => success ?? Code == StatusCodes.OK;
            set => success = new bool?(value);
        }

        /// <summary>
        /// 错误信息。
        /// </summary>
        [XmlElement("msg")]
        public string Msg { get; set; }

        /// <summary>
        /// Utc。
        /// </summary>
        [XmlElement("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 成功。
        /// </summary>
        public static ServResult Ok() => new ServResult();

        /// <summary>
        /// 成功。
        /// </summary>
        /// <param name="data">数据。</param>
        /// <returns></returns>
        public static ServResult<T> Ok<T>(T data) => new ServResult<T>
        {
            Code = StatusCodes.OK,
            Data = data
        };

        /// <summary>
        /// 成功。
        /// </summary>
        /// <param name="data">数据。</param>
        /// <returns></returns>
        public static ServResults<T> Ok<T>(PagedList<T> data) => new ServResults<T>
        {
            Code = StatusCodes.OK,
            Count = data.Count,
            Data = data.ToList()
        };

        /// <summary>
        /// 错误信息。
        /// </summary>
        /// <param name="msg">错误消息。</param>
        /// <param name="errorCode">错误代码。</param>
        /// <returns></returns>
        public static ServResult Error(string msg, int errorCode = StatusCodes.Error)
        {
            return new ServResult
            {
                Code = errorCode,
                Msg = msg
            };
        }

        /// <summary>
        /// 错误信息。
        /// </summary>
        /// <typeparam name="T">实体。</typeparam>
        /// <param name="msg">错误消息。</param>
        /// <param name="errorCode">错误代码。</param>
        /// <returns></returns>
        public static ServResult<T> Error<T>(string msg, int errorCode = StatusCodes.Error)
        {
            return new ServResult<T>
            {
                Code = errorCode,
                Msg = msg
            };
        }

        /// <summary>
        /// 错误信息。
        /// </summary>
        /// <typeparam name="T">实体。</typeparam>
        /// <param name="msg">错误消息。</param>
        /// <param name="errorCode">错误代码。</param>
        /// <returns></returns>
        public static ServResults<T> Errors<T>(string msg, int errorCode = StatusCodes.Error)
        {
            return new ServResults<T>
            {
                Code = errorCode,
                Msg = msg
            };
        }
    }

    /// <summary>
    /// 服务调用数据结果。
    /// </summary>
    /// <typeparam name="T">数据。</typeparam>
    [XmlRoot("xml")]
    public class ServResult<T> : ServResult, IResult<T>, IResult
    {
        /// <summary>
        /// 数据。
        /// </summary>
        [XmlElement("data")]
        public T Data { get; set; }
    }

    /// <summary>
    /// 服务调用数据结果。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    [XmlRoot("xml")]
    public class ServResults<T> : ServResult<List<T>>, IResults<T>, IResult<List<T>>, IResult
    {
        /// <summary>
        /// 总条数。
        /// </summary>
        [XmlElement("count")]
        public int Count { get; set; }
    }
}
