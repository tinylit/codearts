﻿#if NETSTANDARD2_0 || NETCOREAPP3_0
using System;

namespace SkyBuilding.Mvc
{
    /// <summary>
    /// 请求方式
    /// </summary>
    [Flags]
    public enum HttpVerbs
    {
        /// <summary>
        /// Get
        /// </summary>
        GET = 1,
        /// <summary>
        /// Post
        /// </summary>
        POST = 2,
        /// <summary>
        /// Put
        /// </summary>
        PUT = 4,
        /// <summary>
        /// Delete
        /// </summary>
        DELETE = 8,
        /// <summary>
        /// Head
        /// </summary>
        HEAD = 16,
        /// <summary>
        /// Patch
        /// </summary>
        PATCH = 32,
        /// <summary>
        /// Options
        /// </summary>
        OPTIONS = 64
    }
}
#endif