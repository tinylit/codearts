﻿#if NET40_OR_GREATER
using System;
#if NET45_OR_GREATER
using System.Threading.Tasks;
#endif
using System.Web;

namespace CodeArts.Mvc.Builder
{
#if NET40
    /// <summary>
    /// 请求委托。
    /// </summary>
    /// <param name="context">请求上下文。</param>
    /// <returns></returns>
    public delegate void RequestDelegate(HttpContext context);
#else
    /// <summary>
    /// 请求委托。
    /// </summary>
    /// <param name="context">请求上下文。</param>
    /// <returns></returns>
    public delegate Task RequestDelegate(HttpContext context);
#endif
    /// <summary>
    /// 程序构造器。
    /// </summary>
    public interface IApplicationBuilder
    {
        /// <summary>
        /// 使用中间件。
        /// </summary>
        /// <param name="middleware">中间件。</param>
        /// <returns></returns>
        IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);
    }
}
#endif