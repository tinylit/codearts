using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mvc.Core
{
    /// <inheritdoc />
    public class WeatherForecast
    {
        /// <inheritdoc />
        public DateTime? Date { get; set; }

        /// <inheritdoc />
        public int TemperatureC { get; set; }

        /// <inheritdoc />
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        /// <inheritdoc />
        public List<long> Codes { get; set; }

        /// <inheritdoc />
        [Required]
        public string Summary { get; set; }
    }
}
