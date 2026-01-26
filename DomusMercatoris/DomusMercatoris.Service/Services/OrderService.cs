using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Models;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatoris.Service.Services
{
    public class OrderService
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;

        public OrderService(DomusDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<OrderDto>> GetByCompanyIdAsync(int companyId)
        {
            var orders = await _db.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.VariantProduct)
                .Where(o => o.CompanyId == companyId && o.IsPaid)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(MapToDto).ToList();
        }

        private OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                IsPaid = order.IsPaid,
                TotalPrice = order.TotalPrice,
                CompanyId = order.CompanyId,
                UserId = order.UserId,
                FleetingUserId = order.FleetingUserId,
                CargoTrackingId = order.CargoTrackingId,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                OrderItems = order.OrderItems.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name,
                    VariantProductId = i.VariantProductId,
                    VariantName = i.VariantProduct?.Color,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
        }
    }
}
