#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
#else
using SkyBuilding.Serialize.Json;
using System.Net.Http;
using System.Text;
using System.Web.Http.Filters;
#endif
using System;
using System.Net;

namespace SkyBuilding.Mvc
{
#if NETSTANDARD2_0 || NETCOREAPP3_1
    /// <summary> 默认的异常处理 </summary>
    public class DExceptionFilter : IExceptionFilter
    {
        /// <summary> 业务消息过滤 </summary>
        public static Action<DResult> ResultFilter;

        /// <summary>
        /// 错误消息处理
        /// </summary>
        /// <param name="context">异常上下文</param>
        public void OnException(ExceptionContext context)
        {
            var json = Exceptions.ExceptionHandler.Handler(context.Exception);
            if (json == null)
                return;

            ResultFilter?.Invoke(json);

            int code = (int)HttpStatusCode.OK;
            context.Result = new JsonResult(json)
            {
                StatusCode = code
            };
            context.HttpContext.Response.StatusCode = code;
            context.ExceptionHandled = true;
        }
    }
#else
    /// <inheritdoc />
    /// <summary> 默认的异常处理 </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class DExceptionFilterAttribute : ExceptionFilterAttribute
    {
        /// <summary> 业务消息过滤 </summary>
        public static Action<DResult> ResultFilter;

        /// <summary> 异常处理 </summary>
        /// <param name="context">异常上下文</param>
        public override void OnException(HttpActionExecutedContext context)
        {
            var json = Exceptions.ExceptionHandler.Handler(context.Exception);
            if (json == null)
                return;

            ResultFilter?.Invoke(json);

            context.ActionContext.Response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonHelper.ToJson(json), Encoding.UTF8, "application/json")
            };
        }
    }
#endif
}
