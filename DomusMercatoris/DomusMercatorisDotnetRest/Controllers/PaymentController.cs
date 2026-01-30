using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Repositories;
using DomusMercatoris.Core.Models;
using DomusMercatorisDotnetRest.Hubs;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Data;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IGenericRepository<Order> _orderRepository;
        private readonly IHubContext<PaymentHub> _hubContext;
        private readonly DomusMercatorisDotnetRest.Services.OrdersService _ordersService;

        public PaymentController(IGenericRepository<Order> orderRepository, IHubContext<PaymentHub> hubContext, DomusMercatorisDotnetRest.Services.OrdersService ordersService)
        {
            _orderRepository = orderRepository;
            _hubContext = hubContext;
            _ordersService = ordersService;
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
            var order = await _ordersService.ProcessPaymentAsync(request.OrderId, request.Approved);

            // Notify client (user)
            await _hubContext.Clients.Group(order.Id.ToString()).SendAsync("PaymentStatusChanged", new 
            { 
                OrderId = order.Id, 
                Status = order.Status.ToString(),
                IsApproved = request.Approved
            });

            // Notify Company if Approved
            if (request.Approved)
            {
                await _hubContext.Clients.Group($"Company-{order.CompanyId}").SendAsync("NewOrderReceived", new 
                {
                    OrderId = order.Id,
                    CreatedAt = order.CreatedAt,
                    TotalPrice = order.TotalPrice,
                    Status = order.Status.ToString()
                });
            }

            return Ok(new { Message = "Processed" });
        }

        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeRequest request)
        {
            var order = await _ordersService.VerifyPaymentCodeAsync(request.OrderId, request.Code);

            // Notify client (user)
            await _hubContext.Clients.Group(order.Id.ToString()).SendAsync("PaymentStatusChanged", new 
            { 
                OrderId = order.Id, 
                Status = order.Status.ToString(),
                IsApproved = true
            });

            // Notify Company
            await _hubContext.Clients.Group($"Company-{order.CompanyId}").SendAsync("NewOrderReceived", new 
            {
                OrderId = order.Id,
                CreatedAt = order.CreatedAt,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString()
            });

            return Ok(new { Message = "Payment Approved" });
        }

        [HttpPost("reject/{orderId}")]
        public async Task<IActionResult> RejectPayment(long orderId)
        {
            var order = await _ordersService.RejectPaymentAsync(orderId);

            // Notify client (user)
            await _hubContext.Clients.Group(order.Id.ToString()).SendAsync("PaymentStatusChanged", new 
            { 
                OrderId = order.Id, 
                Status = order.Status.ToString(),
                IsApproved = false
            });

            return Ok(new { Message = "Payment Rejected" });
        }
    }

    public class ProcessPaymentRequest
    {
        public long OrderId { get; set; }
        public bool Approved { get; set; }
    }

    public class VerifyCodeRequest
    {
        public long OrderId { get; set; }
        public required string Code { get; set; }
    }
}
