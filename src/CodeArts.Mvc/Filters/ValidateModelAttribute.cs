using System;
using System.Net;
using System.Text;
#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
#else
using System.Net.Http;
using System.Web.Http.Filters;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using CodeArts.Serialize.Json;
#endif

namespace CodeArts.Mvc.Filters
{
    /// <summary> 模型验证过滤器 </summary>
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// 出错时验证。
        /// </summary>
        /// <param name="context">异常上下文</param>
#if NETSTANDARD2_0 || NETCOREAPP3_1
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            //? 验证是否通过
            if (context.ModelState.IsValid)
                return;

            //? 0条错误信息
            if (context.ModelState.ErrorCount == 0)
                return;

            bool flag = false;

            var sb = new StringBuilder();

            foreach (ModelStateEntry entry in context.ModelState.Values)
            {
                foreach (ModelError item in entry.Errors)
                {
                    string message = item.ErrorMessage;

                    if (message.IsEmpty())
                    {
                        if (item.Exception is null)
                        {
                            continue;
                        }

                        message = item.Exception.Message;
                    }

                    if (flag)
                    {
                        sb.AppendLine();
                    }
                    else
                    {
                        flag = true;
                    }

                    sb.Append(message);
                }
            }

            int code = (int)HttpStatusCode.OK;

            context.Result = new JsonResult(DResult.Error(sb.ToString(), StatusCodes.RequestForbidden))
            {
                StatusCode = code
            };

            context.HttpContext.Response.StatusCode = code;
        }

        /// <summary>
        /// 出错时结果验证。
        /// </summary>
        /// <param name="context">异常上下文</param>
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            //? 验证是否通过
            if (context.ModelState.IsValid)
                return;

            //? 0条错误信息
            if (context.ModelState.ErrorCount == 0)
                return;

            bool flag = false;

            var sb = new StringBuilder();

            foreach (ModelStateEntry entry in context.ModelState.Values)
            {
                foreach (ModelError item in entry.Errors)
                {
                    string message = item.ErrorMessage;

                    if (message.IsEmpty())
                    {
                        if (item.Exception is null)
                        {
                            continue;
                        }

                        message = item.Exception.Message;
                    }

                    if (flag)
                    {
                        sb.AppendLine();
                    }
                    else
                    {
                        flag = true;
                    }

                    sb.Append(message);
                }
            }

            int code = (int)HttpStatusCode.OK;

            context.Result = new JsonResult(DResult.Error(sb.ToString(), StatusCodes.RequestForbidden))
            {
                StatusCode = code
            };

            context.HttpContext.Response.StatusCode = code;
        }

#else
        public override void OnActionExecuting(HttpActionContext context)
        {
            //? 验证是否通过
            if (context.ModelState.IsValid)
                return;
            //? 0条错误信息
            if (context.ModelState.Count == 0)
                return;

            bool flag = false;

            var sb = new StringBuilder();


            foreach (ModelState entry in context.ModelState.Values)
            {
                foreach (ModelError item in entry.Errors)
                {
                    string message = item.ErrorMessage;

                    if (message.IsEmpty())
                    {
                        if (item.Exception is null)
                        {
                            continue;
                        }

                        message = item.Exception.Message;
                    }

                    if (flag)
                    {
                        sb.AppendLine();
                    }
                    else
                    {
                        flag = true;
                    }

                    sb.Append(message);
                }
            }

            context.Response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonHelper.ToJson(DResult.Error(sb.ToString(), StatusCodes.RequestForbidden)), Encoding.UTF8, "application/json")
            };
        }
#endif
    }
}
