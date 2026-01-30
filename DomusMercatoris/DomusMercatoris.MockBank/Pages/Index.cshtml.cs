using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Models;
using DomusMercatoris.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace DomusMercatoris.MockBank.Pages
{
    public class IndexModel : PageModel
    {
        private readonly DomusDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public IndexModel(DomusDbContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public List<Order> PendingOrders { get; set; } = new List<Order>();

        public async Task OnGetAsync()
        {
            var orders = await _context.Orders
                .Where(s => s.Status == OrderStatus.PaymentPending)
                .OrderByDescending(s => s.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            foreach (var order in orders)
            {
                if (string.IsNullOrEmpty(order.PaymentCode))
                {
                    order.PaymentCode = Random.Shared.Next(100000, 999999).ToString();
                }
            }

            PendingOrders = orders;
        }

        public async Task<IActionResult> OnPostApproveAsync(long id)
        {
            return await ProcessPayment(id, true);
        }

        public async Task<IActionResult> OnPostRejectAsync(long id)
        {
            return await ProcessPayment(id, false);
        }

        private async Task<IActionResult> ProcessPayment(long id, bool approved)
        {
            // Simulate Webhook Call to API
            var client = _httpClientFactory.CreateClient();
            var payload = new { OrderId = id, Approved = approved };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // Use configured API URL
            var apiUrl = _configuration["ApiUrl"];
            if (string.IsNullOrEmpty(apiUrl))
            {
                TempData["Error"] = "ApiUrl is not configured in appsettings.json";
                return RedirectToPage();
            }

            try 
            {
                var response = await client.PostAsync($"{apiUrl}/api/Payment/process", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = approved ? "Payment Approved" : "Payment Rejected";
                }
                else
                {
                    TempData["Error"] = $"Failed to process payment via Webhook: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                 TempData["Error"] = $"Webhook Error: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
