using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Core.Models;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace MVC.Pages.MockBank
{
    [Authorize(Roles = "Rex")]
    public class IndexModel : PageModel
    {
        private readonly DomusDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(DomusDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public List<Order> PendingOrders { get; set; } = new List<Order>();

        public async Task OnGetAsync()
        {
            PendingOrders = await _context.Orders
                .Where(s => s.Status == OrderStatus.PaymentPending)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
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

            // Assuming API is running on localhost:5280
            var response = await client.PostAsync("http://localhost:5280/api/Payment/process", content);

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
