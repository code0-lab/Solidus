using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Models;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Core.Exceptions;

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

            // Check for existing pending orders for logged-in users
            if (dto.UserId.HasValue)
            {
                var hasPendingOrder = await _db.Orders
                    .AnyAsync(o => o.UserId == dto.UserId && o.Status == OrderStatus.PaymentPending);
                
                if (hasPendingOrder)
                {
                    throw new InvalidOperationException("You already have a pending payment session. Please complete or cancel it before starting a new one.");
                }
            }

            using var transaction = await _db.Database.BeginTransactionAsync();

            try 
            {
                // Validate Stock and Adjust Cart if necessary
                var adjustments = new List<StockAdjustment>();
                bool stockAdjusted = false;

                foreach (var it in dto.Items)
                {
                    var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == it.ProductId);
                    if (product == null) continue;

                    if (product.Quantity < it.Quantity)
                    {
                        stockAdjusted = true;
                        adjustments.Add(new StockAdjustment
                        {
                            ProductId = product.Id,
                            VariantProductId = it.VariantProductId,
                            ProductName = product.Name,
                            RequestedQuantity = it.Quantity,
                            AvailableQuantity = product.Quantity
                        });

                        // Update cart item if User is logged in
                        if (dto.UserId.HasValue)
                        {
                            var cartItem = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == dto.UserId && c.ProductId == it.ProductId && c.VariantProductId == it.VariantProductId);
                            if (cartItem != null)
                            {
                                cartItem.Quantity = product.Quantity;
                                if (cartItem.Quantity <= 0)
                                {
                                    _db.CartItems.Remove(cartItem);
                                }
                            }
                        }
                    }
                }

                if (stockAdjusted)
                {
                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync(); // Commit cart changes
                    throw new StockInsufficientException(adjustments);
                }

                // Proceed with Order Creation and Stock Deduction
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
                    PaymentCode = Random.Shared.Next(100000, 999999).ToString()
                };
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                decimal total = 0;
                foreach (var it in dto.Items)
                {
                    var product = await _db.Products.SingleOrDefaultAsync(p => p.Id == it.ProductId && p.CompanyId == dto.CompanyId);
                    if (product == null) continue;

                    // Double check stock (concurrency)
                    if (product.Quantity < it.Quantity)
                    {
                        throw new InvalidOperationException($"Stock insufficient for {product.Name}.");
                    }

                    // Deduct Stock
                    product.Quantity -= it.Quantity;
                    _db.Entry(product).State = EntityState.Modified;

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
                
                await transaction.CommitAsync();
                return MapToDto(order);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<OrderDto> MarkPaidAsync(long id)
        {
            var order = await _db.Orders.Include(s => s.OrderItems).SingleOrDefaultAsync(s => s.Id == id);
            if (order == null) throw new NotFoundException($"Order {id} not found.");
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

        public async Task<OrderDto> GetAsync(long id)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.VariantProduct)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) throw new NotFoundException($"Order {id} not found.");

            return MapToDto(order);
        }

        public async Task<CargoTracking> GetTrackingAsync(long orderId)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null || order.CargoTrackingId == null) throw new NotFoundException($"Tracking for order {orderId} not found.");

            var tracking = await _db.CargoTrackings.FindAsync(order.CargoTrackingId);
            if (tracking == null) throw new NotFoundException($"Tracking entry {order.CargoTrackingId} not found.");
            return tracking;
        }

        public async Task<PaginatedResult<OrderDto>> GetByUserIdAsync(long userId, int pageNumber = 1, int pageSize = 10, string? tab = null)
        {
            var query = _db.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId && o.Status != OrderStatus.Created && o.Status != OrderStatus.PaymentPending);

            if (tab == "failed-orders")
            {
                query = query.Where(o => o.Status == OrderStatus.PaymentFailed);
            }
            else if (tab == "orders")
            {
                query = query.Where(o => o.Status != OrderStatus.PaymentFailed);
            }

            var totalCount = await query.CountAsync();

            var orders = await query
                .Include(o => o.CargoTracking)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.VariantProduct)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<OrderDto>
            {
                Items = orders.Select(MapToDto).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<Order> ProcessPaymentAsync(long orderId, bool approved)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) throw new NotFoundException($"Order {orderId} not found.");

            if (order.Status != OrderStatus.PaymentPending)
            {
                if (approved && order.Status == OrderStatus.PaymentApproved) return order; // Idempotent success
                if (!approved && order.Status == OrderStatus.PaymentFailed) return order; // Idempotent success
                
                throw new InvalidOperationException($"Order status is {order.Status}, cannot process payment.");
            }

            if (approved)
            {
                await ApproveOrderAsync(order);
            }
            else
            {
                await RestoreStockAsync(order);
                order.Status = OrderStatus.PaymentFailed;
            }

            _db.Orders.Update(order);
            await _db.SaveChangesAsync();
            return order;
        }

        public async Task<Order> VerifyPaymentCodeAsync(long orderId, string code)
        {
             var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) throw new NotFoundException($"Order {orderId} not found.");

            if (order.Status != OrderStatus.PaymentPending)
            {
                 if (order.Status == OrderStatus.PaymentApproved && order.PaymentCode == code) return order;
                 throw new InvalidOperationException("Order is not pending payment.");
            }

            if (order.PaymentCode != code)
            {
                throw new BadRequestException("Invalid payment code.");
            }

            await ApproveOrderAsync(order);
            _db.Orders.Update(order);
            await _db.SaveChangesAsync();
            return order;
        }

        public async Task<Order> RejectPaymentAsync(long orderId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) throw new NotFoundException($"Order {orderId} not found.");

            if (order.Status != OrderStatus.PaymentPending)
                throw new InvalidOperationException("Order is not pending.");

            await RestoreStockAsync(order);
            order.Status = OrderStatus.PaymentFailed;
            
            _db.Orders.Update(order);
            await _db.SaveChangesAsync();
            return order;
        }

        private async Task ApproveOrderAsync(Order order)
        {
            order.Status = OrderStatus.PaymentApproved;
            order.IsPaid = true;
            order.PaidAt = DateTime.UtcNow;
            _db.Entry(order).State = EntityState.Modified;
        }

        private async Task RestoreStockAsync(Order order)
        {
            if (order.Status == OrderStatus.PaymentFailed) return;

            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.Quantity += item.Quantity;
                        _db.Entry(item.Product).State = EntityState.Modified;
                    }
                }
            }
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
                CargoTrackingNumber = order.CargoTracking?.TrackingNumber,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                PaymentCode = order.PaymentCode,
                OrderItems = order.OrderItems.Select(i => new OrderItemDto
                {
                    Id = i.Id,
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