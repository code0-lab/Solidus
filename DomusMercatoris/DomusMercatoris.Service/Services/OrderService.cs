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

        public async Task<(List<Order> Items, int TotalCount)> GetPagedByCompanyIdAsync(int companyId, int pageNumber, int pageSize)
        {
            var query = _db.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && o.IsPaid);

            var totalCount = await query.CountAsync();

            var items = await query
                .Include(o => o.User)
                .Include(o => o.FleetingUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.VariantProduct)
                .Include(o => o.CargoTracking)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Order?> GetByIdAsync(long id)
        {
            return await _db.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.FleetingUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.VariantProduct)
                .Include(o => o.CargoTracking)
                .SingleOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Order>> GetPendingOrdersAsync()
        {
            return await _db.Orders
                .AsNoTracking()
                .Where(s => s.Status == OrderStatus.PaymentPending)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
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
