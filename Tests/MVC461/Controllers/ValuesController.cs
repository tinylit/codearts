using log4net.Core;
using Mvc461.Domain;
using Mvc461.Domain.Entities;
using CodeArts;
using CodeArts.Exceptions;
using CodeArts.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Http;

namespace MVC461.Controllers
{
    /// <inheritdoc />
    public enum TestEnum
    {
        Normal
    }
    /// <inheritdoc />
    public interface IDependency
    {
        /// <inheritdoc />
        bool AddUser(UserDto user);
    }
    /// <inheritdoc />
    public class Dependency : IDependency
    {
        private readonly UserRepository user;

        /// <inheritdoc />
        public Dependency(UserRepository user)
        {
            this.user = user;
        }

        /// <inheritdoc />
        public bool AddUser(UserDto user)
        {
            var data = this.user.Where(x => x.Id == 10000).FirstOrDefault();

            return this.user.AsInsertable(Mapper.Map<User>(user)).ExecuteCommand() > 0;
        }
    }

    /// <inheritdoc />
    public class UserDto
    {
        /// <summary>
        /// ID。
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// 机构ID。
        /// </summary>
        public ulong OrgId { get; set; }
        /// <summary>
        /// 公司ID。
        /// </summary>
        public long CompanyId { get; set; }
        /// <summary>
        /// 账户。
        /// </summary>
        [Display(Name = "用户账户")]
        public string Account { get; set; }
        /// <summary>
        /// 用户名称。
        /// </summary>
        [Display(Name = "用户名称")]
        public string Name { get; set; }
    }

    public class ValueModel
    {
        [Required]
        public string value { get; set; }
    }

    /// <inheritdoc />
    public class ValuesController : BaseController
    {
        private readonly IDependency dependency;

        /// <inheritdoc />
        public ValuesController(IDependency dependency)
        {
            this.dependency = dependency;
        }
        /// <inheritdoc />
        // GET api/values
        [HttpGet]
        public IEnumerable<object> Get()
        {
            return new object[] { TestEnum.Normal, 1000000uL, 10000000000000000000uL };
        }

        /// <inheritdoc />
        // GET api/values/5
        [HttpGet]
        public long Get(long id)
        {
            return id;
        }
        /// <inheritdoc />
        // POST api/values
        [HttpPost]
        public DResult Post([FromBody] UserDto value)
        {
            return dependency.AddUser(value) ? DResult.Ok() : DResult.Error("新增数据失败!");
        }
        /// <inheritdoc />
        // PUT api/values/5
        [HttpPut]
        public void Put(int id, [FromBody] ValueModel value)
        {

        }
        /// <inheritdoc />
        // DELETE api/values/5
        [Authorize]
        [HttpDelete]
        public void Delete(int id)
        {
            throw new BusiException("认证测试成功");
        }
        /// <inheritdoc />
        [HttpGet]
        [ActionName("login")]
        public DResult Login(string account, [Required][MinLength(15)] string password)
        {
            return DResult.Ok(new
            {
                id = 100000,
                name = account,
                account = account
            });
        }

        [HttpGet]
        [ActionName("sms")]
        public DResult AuthCode(string mobile, string authCode)
        {
            return DResult.Ok(new Random().Next(1000, 10000));
        }
    }
}
