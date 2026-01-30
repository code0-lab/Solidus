using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Models;
using DomusMercatoris.Data;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DomusMercatoris.Service.Services
{
    public class RefundService
    {
        private readonly DomusDbContext _context;

        public RefundService(DomusDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateRefundRequestAsync(long userId, CreateRefundRequestDto dto)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.Id == dto.OrderItemId);

            if (orderItem == null || orderItem.Order.UserId != userId)
                return false;

            // Cannot refund/cancel if order is currently being shipped
            if (orderItem.Order.Status == OrderStatus.Shipped)
                return false;

            if (dto.Quantity <= 0 || dto.Quantity > orderItem.Quantity)
                return false;

            var existingRefunds = await _context.RefundRequests
                .Where(r => r.OrderItemId == dto.OrderItemId && r.Status != RefundStatus.Rejected)
                .SumAsync(r => r.Quantity);
            
            if (existingRefunds + dto.Quantity > orderItem.Quantity)
                return false;

            var request = new RefundRequest
            {
                OrderItemId = dto.OrderItemId,
                Quantity = dto.Quantity,
                Reason = dto.Reason,
                Status = RefundStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.RefundRequests.Add(request);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RefundRequestDto>> GetUserRefundsAsync(long userId)
        {
            return await _context.RefundRequests
                .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
                .Where(r => r.OrderItem.Order.UserId == userId)
                .Select(r => new RefundRequestDto
                {
                    Id = r.Id,
                    OrderItemId = r.OrderItemId,
                    ProductName = r.OrderItem.Product.Name,
                    Quantity = r.Quantity,
                    RefundAmount = r.Quantity * r.OrderItem.UnitPrice,
                    Reason = r.Reason,
                    Status = r.Status,
                    RejectionReason = r.RejectionReason,
                    CreatedAt = r.CreatedAt
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<RefundRequestDto>> GetCompanyRefundsAsync(int companyId)
        {
             return await _context.RefundRequests
                .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Product)
                .Include(r => r.OrderItem.Order)
                .Where(r => r.OrderItem.Order.CompanyId == companyId)
                .Select(r => new RefundRequestDto
                {
                    Id = r.Id,
                    OrderItemId = r.OrderItemId,
                    ProductName = r.OrderItem.Product.Name,
                    Quantity = r.Quantity,
                    RefundAmount = r.Quantity * r.OrderItem.UnitPrice,
                    Reason = r.Reason,
                    Status = r.Status,
                    RejectionReason = r.RejectionReason,
                    CreatedAt = r.CreatedAt
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ProcessRefundRequestAsync(int companyId, UpdateRefundStatusDto dto)
        {
            var request = await _context.RefundRequests
                .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Order)
                .Include(r => r.OrderItem)
                .FirstOrDefaultAsync(r => r.Id == dto.RefundRequestId);

            if (request == null || request.OrderItem.Order.CompanyId != companyId)
                return false;

            if (request.Status != RefundStatus.Pending)
                return false;

            if (dto.IsApproved)
            {
                request.Status = RefundStatus.Approved;
                request.UpdatedAt = DateTime.UtcNow;

                // Increase inventory
                var product = await _context.Products.FindAsync(request.OrderItem.ProductId);
                if (product != null)
                {
                    product.Quantity += request.Quantity;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                    return false; // Reason mandatory

                request.Status = RefundStatus.Rejected;
                request.RejectionReason = dto.RejectionReason;
                request.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetPendingRefundsCountAsync(int companyId)
        {
            return await _context.RefundRequests
                .Where(r => r.OrderItem.Order.CompanyId == companyId && r.Status == RefundStatus.Pending)
                .CountAsync();
        }
    }
}
