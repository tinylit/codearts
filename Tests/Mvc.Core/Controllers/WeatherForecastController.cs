using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SkyBuilding;
using SkyBuilding.Mvc;

namespace Mvc.Core.Controllers
{
    public interface IDependency
    {

    }

    public class Dependency : IDependency
    {

    }

    [Route("[controller]")]
    public class WeatherForecastController : BaseController
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public WeatherForecastController(IDependency dependency)
        {

        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpPut]
        [Authorize]
        public DResult<WeatherForecast> Put([FromBody]WeatherForecast weather)
        {
            return weather;
        }

        [HttpGet("login")]
        public DResult Login(string accont, string password)
        {
            return DResult.Ok(new
            {
                id = 100000,
                name = accont
            });
        }
    }
}
