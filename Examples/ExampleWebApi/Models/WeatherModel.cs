using MongoObject.Core.Attributes;

namespace ExampleWebApi.Models
{
    [MongoObject]
    public partial class WeatherModel
    {
        public partial DateOnly Date { get; set; }

        public partial int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public partial string? Summary { get; set; }
    }
}
