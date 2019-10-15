using SkyBuilding;
using SkyBuilding.Exceptions;
using SkyBuilding.Mvc;
using System.Collections.Generic;
using System.Web.Http;

namespace Mvc45.Controllers
{
    ///<summary></summary>
    public interface IDependency
    {

    }

    public class Dependency : IDependency
    {

    }


    public class ValuesController : BaseController
    {
        public ValuesController(IDependency dependency)
        {

        }

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

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete]
        [JwtAuthorize]
        public void Delete(int id)
        {
            throw new BusiException("认证测试成功");
        }

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
