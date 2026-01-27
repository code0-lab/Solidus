using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using DomusMercatoris.Service.Services;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace MVC.Pages.MockBank
{
    [Authorize(Roles = "Rex")]
    public class IndexModel : PageModel
    {
        private readonly OrderService _orderService;
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(OrderService orderService, IHttpClientFactory httpClientFactory)
        {
            _orderService = orderService;
            _httpClientFactory = httpClientFactory;
        }

        public List<Order> PendingOrders { get; set; } = new List<Order>();

        public async Task OnGetAsync()
        {
            PendingOrders = await _orderService.GetPendingOrdersAsync();
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

            // Use current request scheme and host to construct the URL dynamically
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var response = await client.PostAsync($"{baseUrl}/api/Payment/process", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = approved ? "Payment Approved" : "Payment Rejected";
            }
            else
            {
                TempData["Error"] = "Failed to process payment via Webhook";
            }

            return RedirectToPage();
        }
    }
}
