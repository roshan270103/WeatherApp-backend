using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Weatherapibackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey = "898c51e8b3072bda4577b3aff97c30e7"; // Replace with your real API key

        public WeatherController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetWeather([FromQuery] string city)
        {
            if (string.IsNullOrWhiteSpace(city)) return BadRequest("City is required");

            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={_apiKey}&units=metric";

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Error fetching weather data");
            }

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }

        // New forecast endpoint for 5 day / 3 hour forecast
        [HttpGet("forecast")]
        public async Task<IActionResult> GetForecast([FromQuery] string city)
        {
            if (string.IsNullOrWhiteSpace(city)) return BadRequest("City is required");

            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://api.openweathermap.org/data/2.5/forecast?q={city}&appid={_apiKey}&units=metric";

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Error fetching forecast data");
            }

            var content = await response.Content.ReadAsStringAsync();

            // parse and distill content to daily summary (one forecast per day)
            var forecastResponse = JsonSerializer.Deserialize<OpenWeatherForecastResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (forecastResponse == null)
                return BadRequest("Invalid forecast data");

            // Group the 3-hour entries into days and take min/max temps etc.
            var dailyForecasts = forecastResponse.List
                .GroupBy(f => f.DtTxt.Date)
                .Select(g => new ForecastDay
                {
                    Date = g.Key,
                    MinTemp = g.Min(x => x.Main.TempMin),
                    MaxTemp = g.Max(x => x.Main.TempMax),
                    Description = g.First().Weather[0].Description,
                    Icon = g.First().Weather[0].Icon
                })
                .Take(3) // take next 5 days
                .ToList();

            return Ok(dailyForecasts);
        }

        // Classes to parse forecast response from OpenWeather API

        public class OpenWeatherForecastResponse
        {
            public List<ForecastListItem> List { get; set; }
        }
        public class ForecastListItem
        {
            public ForecastMain Main { get; set; }
            public List<ForecastWeather> Weather { get; set; }

            [JsonPropertyName("dt_txt")]
            public string DtTxtRaw { get; set; } // Raw string for debugging

            [JsonIgnore]
            public DateTime DtTxt
            {
                get
                {
                    DateTime dt;
                    if (DateTime.TryParse(DtTxtRaw, out dt))
                        return dt;
                    return DateTime.MinValue;
                }
            }
        }

        public class ForecastMain
        {
            [JsonPropertyName("temp_min")]
            public double TempMin { get; set; }

            [JsonPropertyName("temp_max")]
            public double TempMax { get; set; }
        }
        public class ForecastWeather
        {
            public string Description { get; set; }
            public string Icon { get; set; }
        }

        // Forecast day model sent to frontend
        public class ForecastDay
        {
            public System.DateTime Date { get; set; }
            public double MinTemp { get; set; }
            public double MaxTemp { get; set; }
            public string Description { get; set; }
            public string Icon { get; set; }
        }
    }
}
