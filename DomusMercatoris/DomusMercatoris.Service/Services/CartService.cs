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
            return await _db.CartItems
                .Where(c => c.UserId == userId)
                .AsNoTracking()
                .Select(c => new CartItemDto
                {
                    Id = c.Id,
                    ProductId = c.ProductId,
                    ProductName = c.Product.Name,
                    ProductImage = (c.VariantProduct != null ? c.VariantProduct.CoverImage : (c.Product.Images != null && c.Product.Images.Count > 0 ? c.Product.Images[0] : string.Empty)) ?? string.Empty,
                    Price = c.VariantProduct != null ? c.VariantProduct.Price : c.Product.Price,
                    VariantProductId = c.VariantProductId,
                    VariantColor = c.VariantProduct != null ? c.VariantProduct.Color : null,
                    Quantity = c.Quantity,
                    CompanyId = c.Product.CompanyId
                })
                .ToListAsync();
        }

        public async Task<string?> AddToCartAsync(long userId, AddToCartDto dto)
        {
            if (dto.Quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.", nameof(dto.Quantity));
            }

            var product = await _db.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                throw new ArgumentException("Product not found.", nameof(dto.ProductId));
            }

            var existingItem = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == dto.ProductId && c.VariantProductId == dto.VariantProductId);

            int currentCartQuantity = existingItem?.Quantity ?? 0;
            int newQuantity = currentCartQuantity + dto.Quantity;
            string? warningMessage = null;

            if (newQuantity > product.Quantity)
            {
                // Cap the quantity to available stock
                int quantityToAdd = product.Quantity - currentCartQuantity;
                if (quantityToAdd <= 0)
                {
                    return $"Current stock is {product.Quantity}. You already have {currentCartQuantity} in cart.";
                }
                
                newQuantity = product.Quantity; // Total quantity in cart becomes max stock
                warningMessage = $"Current stock is {product.Quantity}. Added max available amount.";
            }

            if (existingItem != null)
            {
                existingItem.Quantity = newQuantity;
            }
            else
            {
                var item = new CartItem
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    VariantProductId = dto.VariantProductId,
                    Quantity = newQuantity
                };
                _db.CartItems.Add(item);
            }
            await _db.SaveChangesAsync();

            return warningMessage;
        }

        public async Task<string?> UpdateQuantityAsync(long userId, long itemId, int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
            }

            var item = await _db.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == itemId && c.UserId == userId);
            
            if (item != null)
            {
                string? warningMessage = null;
                if (quantity > item.Product.Quantity)
                {
                    quantity = item.Product.Quantity;
                    warningMessage = $"Current stock is {item.Product.Quantity}. Updated to max available amount.";
                }

                item.Quantity = quantity;
                await _db.SaveChangesAsync();
                return warningMessage;
            }
            return null;
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
            await _db.CartItems
                .Where(c => c.UserId == userId)
                .ExecuteDeleteAsync();
        }
    }
}