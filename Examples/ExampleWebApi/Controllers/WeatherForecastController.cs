using ExampleWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using MongoObject.Core.Data;
using MongoObject.Core.Interfaces;

namespace ExampleWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController(IDocumentMonitor<WeatherModel> monitor) : ControllerBase
    {
        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherModel>> Get()
        {
            var results = await monitor.Search()
                .WithLimit(5)
                .WithQuery(f => {
                    f.TemperatureC = f.TemperatureC.Gte(25);
                    f.Date = f.Date.Gte(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)));
                });

            return results;
        }
    }
}
