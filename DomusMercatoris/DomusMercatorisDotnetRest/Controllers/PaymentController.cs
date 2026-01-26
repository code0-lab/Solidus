using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Repositories;
using DomusMercatoris.Core.Models;
using DomusMercatorisDotnetRest.Hubs;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Service.DTOs;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IGenericRepository<Order> _orderRepository;
        private readonly IHubContext<PaymentHub> _hubContext;

        public PaymentController(IGenericRepository<Order> orderRepository, IHubContext<PaymentHub> hubContext)
        {
            _orderRepository = orderRepository;
            _hubContext = hubContext;
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetPendingOrders()
        {
            // Note: In a real app, use a DTO. Here simplifying for demo.
            // Also GenericRepository usually returns IEnumerable, but we might need Include.
            // Assuming GetAllAsync returns all, filtering in memory for now or adding a specific method to repository is better.
            // But since IGenericRepository is generic, I might not have custom query methods easily exposed unless I cast or use IQueryable.
            // Let's check IGenericRepository interface again.
            // It has GetAllAsync().
            
            var allOrders = await _orderRepository.GetAllAsync();
            var pendingOrders = allOrders.Where(s => s.Status == OrderStatus.PaymentPending).ToList();
            
            // Map to DTO (simplified)
            var dtos = pendingOrders.Select(s => new OrderDto
            {
                Id = s.Id,
                TotalPrice = s.TotalPrice,
                IsPaid = s.IsPaid,
                Status = s.Status, // Need to add Status to SaleDto
                CreatedAt = s.CreatedAt
            });

            return Ok(dtos);
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null) return NotFound();

            if (request.Approved)
            {
                order.Status = OrderStatus.PaymentApproved;
                order.IsPaid = true;
                order.PaidAt = DateTime.UtcNow;
            }
            else
            {
                order.Status = OrderStatus.PaymentFailed;
            }

            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            // Notify client
            await _hubContext.Clients.Group(order.Id.ToString()).SendAsync("PaymentStatusChanged", new 
            { 
                OrderId = order.Id, 
                Status = order.Status.ToString(),
                IsApproved = request.Approved
            });

            return Ok(new { Message = "Processed" });
        }
    }

    public class ProcessPaymentRequest
    {
        public long OrderId { get; set; }
        public bool Approved { get; set; }
    }
}
