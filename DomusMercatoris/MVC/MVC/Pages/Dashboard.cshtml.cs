using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using DomusMercatorisDotnetMVC.Services;
using System.Linq;
using DomusMercatorisDotnetMVC.Dto.CommentsDto;
using OrderService = DomusMercatoris.Service.Services.OrderService;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using RefundService = DomusMercatoris.Service.Services.RefundService;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = "Manager,User")]
    public class DashboardModel : PageModel
    {
        private readonly ProductService _productService;
        private readonly UserService _userService;
        private readonly CommentService _commentService;
        private readonly OrderService _orderService;
        private readonly RefundService _refundService;
        private readonly DomusDbContext _db;

        public DashboardModel(ProductService productService, UserService userService, CommentService commentService, OrderService orderService, RefundService refundService, DomusDbContext db)
        {
            _productService = productService;
            _userService = userService;
            _commentService = commentService;
            _orderService = orderService;
            _refundService = refundService;
            _db = db;
        }

        public int CompanyId { get; set; }
        public int ProductCount { get; set; }
        public int WorkerCount { get; set; }
        public int ManagerCount { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int PendingRefundCount { get; set; }
        public List<CommentsDto> RecentComments { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
        public List<Product> LowStockProducts { get; set; } = new();
        public int TotalOrders { get; set; }

        public async Task OnGetAsync()
        {
            var comp = User.FindFirst("CompanyId")?.Value;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var companyId))
            {
                CompanyId = companyId;
            }
            else
            {
                var idClaim = User.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
                {
                    var me = await _userService.GetByIdAsync(userId);
                    if (me != null)
                    {
                        CompanyId = me.CompanyId;
                        ManagerName = me.FirstName + " " + me.LastName;
                    }
                }
            }
            if (CompanyId >= 0)
            {
                ProductCount = await _productService.CountByCompanyAsync(CompanyId);
                PendingRefundCount = await _refundService.GetPendingRefundsCountAsync(CompanyId);
                LowStockProducts = await _productService.GetLowStockProductsAsync(CompanyId);
                var users = await _userService.GetByCompanyAsync(CompanyId);
                ManagerCount = users.Count(u => u.Roles?.Contains("Manager") ?? false);
                WorkerCount = users.Count(u => !(u.Roles?.Contains("Manager") ?? false));
                CompanyName = await _userService.GetCompanyNameAsync(CompanyId) ?? string.Empty;
                var result = await _commentService.GetCommentsForCompanyAsync(CompanyId, 1, 1, 3);
                RecentComments = result.Items;

                var orderResult = await _orderService.GetPagedByCompanyIdAsync(CompanyId, 1, 5);
                TotalOrders = orderResult.TotalCount;
                RecentOrders = orderResult.Items;
            }
        }
    }
}
