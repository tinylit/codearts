using CodeArts;
using CodeArts.Db.EntityFramework;
using CodeArts.Exceptions;
using CodeArts.Middleware;
using CodeArts.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mvc.Core.Domain;
using Mvc.Core.Domain.Entities;
using Mvc.Core.Dtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Mvc.Core.Controllers
{

    /// <inheritdoc />
    public class DependencyInterceptAttribute : InterceptAttribute
    {

        /// <inheritdoc />
        public override void Run(InterceptContext context, Intercept intercept)
        {
             intercept.Run(context);
        }

        /// <inheritdoc />
        public override Task RunAsync(InterceptAsyncContext context, InterceptAsync intercept)
        {
            return intercept.RunAsync(context);
        }

        /// <inheritdoc />
        public override Task<T> RunAsync<T>(InterceptAsyncContext context, InterceptAsync<T> intercept)
        {
            return intercept.RunAsync(context);
        }
    }

    /// <inheritdoc />
    public interface IDependency
    {
        /// <inheritdoc />
        [DependencyIntercept]
        bool AopTest();
    }

    /// <inheritdoc />
    public class Dependency : IDependency
    {
        private readonly UserRepository user;

        /// <inheritdoc />
        public Dependency(UserRepository user, ILogger<Dependency> logger)
        {
            this.user = user;
        }

        /// <inheritdoc />
        public bool AopTest() => true;
    }


    /// <summary>
    /// 默认。
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ValuesController : BaseController<UserDto>
    {
        private readonly IDependency dependency;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="dependency">依赖注入。</param>
        /// <param name="users">用户信息。</param>
        /// <param name="linqUsers">用户。</param>
        public ValuesController(IDependency dependency, UserRepository users, ILinqRepository<FeiUsers, int> linqUsers)
        {
            this.dependency = dependency;

            dependency.AopTest();
        }

        /// <summary>
        /// 获取。
        /// </summary>
        /// <returns></returns>
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        /// <summary>
        /// 获取。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        /// <summary>
        /// 添加。
        /// </summary>
        /// <param name="value"></param>
        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        /// <summary>
        /// 修改。
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        /// <summary>
        /// 删除。
        /// </summary>
        /// <param name="id"></param>
        // DELETE api/values/5
        [Authorize]
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            throw new ServException($"无效{id}!");
        }

        /// <summary>
        /// 登录。
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpGet("login")]
        public DResult Login(string account, [Required] string password)
        {
            dependency.AopTest();

            return DResult.Ok(new
            {
                id = 100000,
                name = account,
                role = "Admin",
                account
            });
        }


        /// <summary>
        /// 登录。
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpGet("register")]
        public DResult Register(string account, string password)
        {
            dependency.AopTest();
            return DResult.Ok(new
            {
                id = 100000,
                name = account,
                account
            });
        }
        /// <summary>
        /// 测试。
        /// </summary>
        /// <param name="weather"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("test")]
        public DResult Test([FromBody] WeatherForecast weather)
        {
            return DResult.Ok(MyUser);
        }

        /// <summary>
        /// 短信验证码。
        /// </summary>
        /// <param name="mobile">手机号。</param>
        /// <param name="authCode">验证码。</param>
        /// <returns></returns>
        [HttpGet("sms")]
        public DResult AuthCode(string mobile, string authCode)
        {
            return DResult.Ok(new Random().Next(1000, 10000));
        }
    }
}
