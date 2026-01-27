using DomusMercatoris.Core.Models;
using DomusMercatoris.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace DomusMercatoris.MockBank.Services
{
    public class TimeoutService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TimeoutService> _logger;

        public TimeoutService(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<TimeoutService> logger)
        {
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessTimeoutsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing timeouts");
                }

                await Task.Delay(1000, stoppingToken); // Check every second
            }
        }

        private async Task ProcessTimeoutsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DomusDbContext>();

            // Find orders pending for more than 40 seconds
            var timeoutThreshold = DateTime.UtcNow.AddSeconds(-40);
            
            var timedOutOrders = await db.Orders
                .Where(o => o.Status == OrderStatus.PaymentPending && o.CreatedAt < timeoutThreshold)
                .ToListAsync(stoppingToken);

            if (!timedOutOrders.Any()) return;

            var client = _httpClientFactory.CreateClient();
            var apiUrl = _configuration["ApiUrl"] ?? "http://localhost:5280";

            foreach (var order in timedOutOrders)
            {
                _logger.LogInformation($"Order {order.Id} timed out. Rejecting...");

                var payload = new { OrderId = order.Id, Approved = false };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync($"{apiUrl}/api/Payment/process", content, stoppingToken);
                    if (!response.IsSuccessStatusCode)
                    {
                         _logger.LogError($"Failed to reject order {order.Id}. Status: {response.StatusCode}");
                    }
                    else
                    {
                         _logger.LogInformation($"Order {order.Id} rejected successfully via API.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception notifying API for order {order.Id}");
                }
            }
        }
    }
}
