using CodeArts;
using CodeArts.Exceptions;
using CodeArts.Mvc;
using System.Collections.Generic;
using System.Web.Http;

namespace Mvc4.Controllers
{
    /// <inheritdoc />
    public interface IDependency
    {

    }
    /// <inheritdoc />
    public class Dependency : IDependency
    {
    }

    /// <inheritdoc />
    public class ValuesController : BaseController
    {
        /// <inheritdoc />
        public ValuesController(IDependency dependency)
        {

        }
        /// <inheritdoc />
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // GET api/values/5
        [HttpGet]
        public string Get(int id)
        {
            return "value";
        }
        /// <inheritdoc />
        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }
        /// <inheritdoc />
        // PUT api/values/5
        [HttpPut]
        public void Put(int id, [FromBody]string value)
        {
        }
        /// <inheritdoc />
        // DELETE api/values/5
        [HttpDelete]
        [Authorize]
        public void Delete(int id)
        {
            throw new BusiException("认证测试成功");
        }
        /// <inheritdoc />
        [HttpGet]
        [ActionName("login")]
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
