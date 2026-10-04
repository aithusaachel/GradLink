using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GradLink.API.Services
{
    public class KeepAliveService : BackgroundService
    {
        private readonly ILogger<KeepAliveService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _pingUrl;

        public KeepAliveService(ILogger<KeepAliveService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            // The Render deployment URL
            _pingUrl = "https://gradlink-f2pd.onrender.com/swagger";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait a few seconds for the application to fully start
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    var response = await client.GetAsync(_pingUrl, stoppingToken);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Keep-alive ping successful at {Time}", DateTime.UtcNow);
                    }
                    else
                    {
                        _logger.LogWarning("Keep-alive ping returned {StatusCode} at {Time}", response.StatusCode, DateTime.UtcNow);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Keep-alive ping failed at {Time}", DateTime.UtcNow);
                }

                // Ping every 10 minutes to prevent Render from spinning down (limit is 15 minutes)
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
