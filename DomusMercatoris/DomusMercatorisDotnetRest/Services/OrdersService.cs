using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Models;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatorisDotnetRest.Services
{
    public class OrdersService
    {
        private readonly DomusDbContext _db;

        public OrdersService(DomusDbContext db)
        {
            _db = db;
        }

        public async Task<OrderDto> CheckoutAsync(OrderCreateDto dto)
        {
            if (dto.UserId == null && dto.FleetingUser == null) throw new ArgumentException("Either UserId or FleetingUser must be provided.");
            long? fleetingUserId = null;
            if (!dto.UserId.HasValue && dto.FleetingUser != null)
            {
                var fu = new FleetingUser
                {
                    Email = dto.FleetingUser.Email,
                    FirstName = dto.FleetingUser.FirstName,
                    LastName = dto.FleetingUser.LastName,
                    Address = dto.FleetingUser.Address
                };
                _db.Add(fu);
                await _db.SaveChangesAsync();
                fleetingUserId = fu.Id;
            }
            var order = new Order
            {
                CompanyId = dto.CompanyId,
                UserId = dto.UserId ?? 0,
                FleetingUserId = fleetingUserId,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.PaymentPending,
                PaymentCode = new Random().Next(100000, 999999).ToString()
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            decimal total = 0;
            foreach (var it in dto.Items)
            {
                var product = await _db.Products.SingleOrDefaultAsync(p => p.Id == it.ProductId && p.CompanyId == dto.CompanyId);
                if (product == null) continue;
                VariantProduct? vp = null;
                if (it.VariantProductId.HasValue)
                {
                    vp = await _db.VariantProducts.SingleOrDefaultAsync(v => v.Id == it.VariantProductId.Value && v.ProductId == product.Id);
                }
                var unitPrice = vp != null ? vp.Price : product.Price;
                var op = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    VariantProductId = vp?.Id,
                    UnitPrice = unitPrice,
                    Quantity = it.Quantity
                };
                total += unitPrice * it.Quantity;
                _db.OrderItems.Add(op);
                order.OrderItems.Add(op);
            }
            order.TotalPrice = total;
            await _db.SaveChangesAsync();
            
            return MapToDto(order);
        }

        public async Task<OrderDto?> MarkPaidAsync(long id)
        {
            var order = await _db.Orders.Include(s => s.OrderItems).SingleOrDefaultAsync(s => s.Id == id);
            if (order == null) return null;
            order.IsPaid = true;
            order.Status = OrderStatus.PaymentApproved;
            order.PaidAt = DateTime.UtcNow;
            var track = new CargoTracking
            {
                TrackingNumber = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                CarrierName = "Domus Cargo",
                Status = CargoStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UserId = order.UserId > 0 ? order.UserId : null,
                FleetingUserId = order.FleetingUserId
            };
            _db.CargoTrackings.Add(track);
            await _db.SaveChangesAsync();
            
            order.CargoTrackingId = track.Id;
            await _db.SaveChangesAsync();
            
            return MapToDto(order);
        }

        public async Task<OrderDto?> GetAsync(long id)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.VariantProduct)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return null;

            return MapToDto(order);
        }

        public async Task<CargoTracking?> GetTrackingAsync(long orderId)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null || order.CargoTrackingId == null) return null;

            return await _db.CargoTrackings.FindAsync(order.CargoTrackingId);
        }

        public async Task<List<OrderDto>> GetByUserIdAsync(long userId)
        {
            var orders = await _db.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.VariantProduct)
                .Where(o => o.UserId == userId && o.Status != OrderStatus.Created && o.Status != OrderStatus.PaymentPending)
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
                PaymentCode = order.PaymentCode,
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