#if NETSTANDARD2_0 || NETSTANDARD2_1
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
#else
using System.Net.Http;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using SkyBuilding.Serialize.Json;
#endif
using SkyBuilding.Log;
using System.Net;

namespace SkyBuilding.Mvc.Filters
{
    /// <summary> 模型验证过滤器 </summary>
#if NETSTANDARD2_0 || NETSTANDARD2_1
    public class ValidateModelAttribute : Microsoft.AspNetCore.Mvc.Filters.ActionFilterAttribute
#else
    public class ValidateModelAttribute : System.Web.Http.Filters.ActionFilterAttribute
#endif
    {
        private readonly ILogger _logger = LogManager.Logger<ValidateModelAttribute>();

#if NETSTANDARD2_0 || NETSTANDARD2_1
        /// <summary>
        /// 出错是验证
        /// </summary>
        /// <param name="context">异常上下文</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            //? 验证是否通过
            if (context.ModelState.IsValid)
                return;

            //? 0条错误信息
            if (context.ModelState.ErrorCount == 0)
                return;

            string message = string.Empty;

            foreach (ModelStateEntry entry in context.ModelState.Values)
            {
                foreach (ModelError item in entry.Errors)
                {
                    message = item.ErrorMessage;

                    if (!string.IsNullOrWhiteSpace(message))
                        goto label_error;

                    if (item.Exception is null)
                        continue;

                    message = item.Exception.Message;

                    _logger.Error(message, item.Exception);

                    goto label_error;
                }
            }

            return;

        label_error:


            int code = (int)HttpStatusCode.OK;
            context.Result = new JsonResult(DResult.Error(message))
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
            string message = string.Empty;

            foreach (ModelState entry in context.ModelState.Values)
            {
                foreach (ModelError item in entry.Errors)
                {
                    message = item.ErrorMessage;

                    if (!string.IsNullOrWhiteSpace(message))
                        goto label_error;

                    if (item.Exception is null)
                        continue;

                    message = item.Exception.Message;

                    _logger.Error(message, item.Exception);

                    goto label_error;
                }
            }

            return;

        label_error:
            context.Response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonHelper.ToJson(DResult.Error(message)), Encoding.UTF8, "application/json")
            };
        }
#endif
    }
}
