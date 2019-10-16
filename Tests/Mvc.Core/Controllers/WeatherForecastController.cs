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
    /// <inheritdoc />
    public interface IDependency
    {

    }
    /// <inheritdoc />
    public class Dependency : IDependency
    {

    }
    /// <inheritdoc />
    [Route("[controller]")]
    public class WeatherForecastController : BaseController
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        /// <inheritdoc />
        public WeatherForecastController(IDependency dependency)
        {

        }
        /// <inheritdoc />
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
        /// <inheritdoc />
        [HttpPut]
        [Authorize]
        public DResult<WeatherForecast> Put([FromBody]WeatherForecast weather)
        {
            return weather;
        }
        /// <inheritdoc />
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
