using AutoMapper;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Models;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

using DomusMercatoris.Service.Interfaces;

namespace DomusMercatoris.Service.Services
{
    public class OrderService
    {
        private readonly DomusDbContext _db;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public OrderService(DomusDbContext db, IMapper mapper, ICurrentUserService currentUserService)
        {
            _db = db;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<(int ActiveCount, int CompletedCount, int RefundedCount)> GetOrderCountsByCompanyIdAsync(int companyId)
        {
            if (_currentUserService.CompanyId.HasValue && companyId != _currentUserService.CompanyId.Value)
            {
                // Force company ID or return zeros/throw
                // Usually for company dashboards, so enforcing is safer.
                companyId = _currentUserService.CompanyId.Value;
            }

            var activeCount = await _db.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && o.IsPaid && !o.IsRefunded && o.Status != OrderStatus.Shipped && o.Status != OrderStatus.Delivered)
                .CountAsync();

            var completedCount = await _db.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && o.IsPaid && !o.IsRefunded && (o.Status == OrderStatus.Shipped || o.Status == OrderStatus.Delivered))
                .CountAsync();

            var refundedCount = await _db.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId && o.IsRefunded)
                .CountAsync();

            return (activeCount, completedCount, refundedCount);
        }

        public async Task<(List<Order> Items, int TotalCount)> GetPagedByCompanyIdAsync(int companyId, int pageNumber, int pageSize, string tab)
        {
            if (_currentUserService.CompanyId.HasValue && companyId != _currentUserService.CompanyId.Value)
            {
                companyId = _currentUserService.CompanyId.Value;
            }

            var query = _db.Orders
                .AsNoTracking()
                .Where(o => o.CompanyId == companyId);

            if (tab == "refunded")
            {
                query = query.Where(o => o.IsRefunded);
            }
            else
            {
                // Ensure we only show paid orders in active/completed tabs, and exclude refunded ones
                query = query.Where(o => o.IsPaid && !o.IsRefunded);

                if (tab == "completed")
                {
                    query = query.Where(o => o.Status == OrderStatus.Shipped || o.Status == OrderStatus.Delivered);
                }
                else // "recent" or default
                {
                    query = query.Where(o => o.Status != OrderStatus.Shipped && o.Status != OrderStatus.Delivered);
                }
            }

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

        public async Task<Order?> GetOrderDetailsForUserAsync(long orderId, long userId, int? companyId)
        {
            if (_currentUserService.CompanyId.HasValue)
            {
                companyId = _currentUserService.CompanyId.Value;
            }

            // Security Check inside the Query (WHERE clause) to prevent Over-Fetching
            var query = _db.Orders
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Where(o => (o.UserId == userId && userId > 0) || (companyId.HasValue && companyId.Value > 0 && o.CompanyId == companyId.Value));

            return await query
                .Include(o => o.User)
                .Include(o => o.FleetingUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.VariantProduct)
                .Include(o => o.CargoTracking)
                .SingleOrDefaultAsync();
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
