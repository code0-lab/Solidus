using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DomusMercatoris.Service.Services
{
    public class CartService
    {
        private readonly DomusDbContext _db;

        public CartService(DomusDbContext db)
        {
            _db = db;
        }

        public async Task<List<CartItemDto>> GetCartAsync(long userId)
        {
            var items = await _db.CartItems
                .Include(c => c.Product)
                .Include(c => c.VariantProduct)
                .Where(c => c.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

            return items.Select(c => new CartItemDto
            {
                Id = c.Id,
                ProductId = c.ProductId,
                ProductName = c.Product.Name,
                ProductImage = c.VariantProduct?.CoverImage ?? (c.Product.Images != null && c.Product.Images.Count > 0 ? c.Product.Images[0] : string.Empty),
                Price = c.VariantProduct?.Price ?? c.Product.Price,
                VariantProductId = c.VariantProductId,
                VariantColor = c.VariantProduct?.Color,
                Quantity = c.Quantity,
                CompanyId = c.Product.CompanyId
            }).ToList();
        }

        public async Task AddToCartAsync(long userId, AddToCartDto dto)
        {
            if (dto.Quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.", nameof(dto.Quantity));
            }

            var existingItem = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == dto.ProductId && c.VariantProductId == dto.VariantProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                var item = new CartItem
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    VariantProductId = dto.VariantProductId,
                    Quantity = dto.Quantity
                };
                _db.CartItems.Add(item);
            }
            await _db.SaveChangesAsync();
        }

        public async Task UpdateQuantityAsync(long userId, long itemId, int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
            }

            var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == itemId && c.UserId == userId);
            if (item != null)
            {
                item.Quantity = quantity;
                await _db.SaveChangesAsync();
            }
        }

        public async Task RemoveFromCartAsync(long userId, long itemId)
        {
            await _db.CartItems
                .Where(c => c.Id == itemId && c.UserId == userId)
                .ExecuteDeleteAsync();
        }

        public async Task SyncCartAsync(long userId, List<SyncCartItemDto> localItems)
        {
            if (localItems == null || !localItems.Any()) return;

            // Fetch existing DB items to memory to avoid N+1
            var dbItems = await _db.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            foreach (var local in localItems)
            {
                var dbItem = dbItems.FirstOrDefault(c => c.ProductId == local.ProductId && c.VariantProductId == local.VariantProductId);
                if (dbItem != null)
                {
                    dbItem.Quantity += local.Quantity;
                }
                else
                {
                    _db.CartItems.Add(new CartItem
                    {
                        UserId = userId,
                        ProductId = local.ProductId,
                        VariantProductId = local.VariantProductId,
                        Quantity = local.Quantity
                    });
                }
            }
            await _db.SaveChangesAsync();
        }

        public async Task ClearCartAsync(long userId)
        {
            var items = await _db.CartItems.Where(c => c.UserId == userId).ToListAsync();
            if (items.Any())
            {
                _db.CartItems.RemoveRange(items);
                await _db.SaveChangesAsync();
            }
        }
    }
}