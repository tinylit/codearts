﻿#if NET45 || NET451 || NET452 || NET461
using System;
using System.Threading.Tasks;
using System.Web;

namespace SkyBuilding.Mvc.Builder
{
    /// <summary>
    /// 请求委托
    /// </summary>
    /// <param name="context">请求上下文</param>
    /// <returns></returns>
    public delegate Task RequestDelegate(HttpContext context);

    /// <summary>
    /// 程序构造器
    /// </summary>
    public interface IApplicationBuilder
    {
        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <param name="middleware">中间件</param>
        /// <returns></returns>
        IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);
    }
}
#endif