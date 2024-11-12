using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;


namespace RateLimiting.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string RateLimitKeyPrefix = "RateLimit_";
        private readonly IMemoryCache _cache;
        private readonly int _maxRequestsPerWindow = 10;
        private readonly TimeSpan _window = TimeSpan.FromSeconds(60);

        public WeatherForecastController(IMemoryCache cache)
        {
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // Get the client identifier (IP address or API key)
            var clientIdentifier = GetClientIdentifier();

            // Check if the rate limit is exceeded
            if (IsRateLimitExceeded(clientIdentifier))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests);
            }

            // If rate limit is not exceeded, process the request
            var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

            // Increment the request count
            IncrementRequestCount(clientIdentifier);

            // Return the response
            return Ok(forecast);
        }

        private string GetClientIdentifier()
        {
            // Use IP address or API key as the client identifier
            // In this example, we're using IP address
            return HttpContext.Connection.RemoteIpAddress.ToString();
        }

        private bool IsRateLimitExceeded(string clientIdentifier)
        {
            var cacheKey = RateLimitKeyPrefix + clientIdentifier;
            var requestCount = _cache.Get<int>(cacheKey);

            if (requestCount >= _maxRequestsPerWindow)
            {
                return true;
            }

            return false;
        }

        private void IncrementRequestCount(string clientIdentifier)
        {
            var cacheKey = RateLimitKeyPrefix + clientIdentifier;
            _cache.Set(cacheKey, _cache.Get<int>(cacheKey) + 1, _window);
        }

        private static string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        //private static readonly string[] Summaries = new[]
        //{
        //    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        //};

        //private readonly ILogger<WeatherForecastController> _logger;

        //public WeatherForecastController(ILogger<WeatherForecastController> logger)
        //{
        //    _logger = logger;
        //}

        //[HttpGet(Name = "GetWeatherForecast")]
        //public IEnumerable<WeatherForecast> Get()
        //{
        //    return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        //    {
        //        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
        //        TemperatureC = Random.Shared.Next(-20, 55),
        //        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        //    })
        //    .ToArray();
        //}
    }
}
