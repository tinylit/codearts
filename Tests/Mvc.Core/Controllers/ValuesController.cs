﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBuilding;
using SkyBuilding.Exceptions;
using SkyBuilding.Mvc;

namespace Mvc.Core.Controllers
{
    /// <inheritdoc />
    public interface IDependency
    {

    }

    /// <inheritdoc />
    public class Dependency : IDependency
    {

    }

    /// <summary>
    /// 默认
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ValuesController : BaseController
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dependency">依赖注入</param>
        public ValuesController(IDependency dependency)
        {

        }

        /// <summary>
        /// 获取
        /// </summary>
        /// <returns></returns>
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        /// <summary>
        /// 获取
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
        /// 添加
        /// </summary>
        /// <param name="value"></param>
        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        /// <summary>
        /// 删除
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
        /// 登录
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpGet("login")]
        public DResult Login(string account, string password)
        {
            return DResult.Ok(new
            {
                id = 100000,
                name = account
            });
        }
    }
}
