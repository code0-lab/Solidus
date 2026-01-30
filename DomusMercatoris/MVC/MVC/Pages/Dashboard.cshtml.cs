using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using DomusMercatorisDotnetMVC.Services;
using System.Linq;
using DomusMercatorisDotnetMVC.Dto.CommentsDto;
using OrderService = DomusMercatoris.Service.Services.OrderService;
using DomusMercatoris.Service.DTOs;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Constants;
using DomusMercatoris.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using RefundService = DomusMercatoris.Service.Services.RefundService;
using DashboardService = DomusMercatoris.Service.Services.DashboardService;
using DomusMercatorisDotnetMVC.Dto.ProductDto;
using AutoMapper;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Roles = AppConstants.Roles.Manager + "," + AppConstants.Roles.User)]
    public class DashboardModel : PageModel
    {
        private readonly ProductService _productService;
        private readonly UserService _userService;
        private readonly CommentService _commentService;
        private readonly OrderService _orderService;
        private readonly RefundService _refundService;
        private readonly DashboardService _dashboardService;
        private readonly IMapper _mapper;

        public DashboardModel(
            ProductService productService, 
            UserService userService, 
            CommentService commentService, 
            OrderService orderService, 
            RefundService refundService, 
            DashboardService dashboardService,
            IMapper mapper)
        {
            _productService = productService;
            _userService = userService;
            _commentService = commentService;
            _orderService = orderService;
            _refundService = refundService;
            _dashboardService = dashboardService;
            _mapper = mapper;
        }

        public int CompanyId { get; set; }
        public int ProductCount { get; set; }
        public int WorkerCount { get; set; }
        public int ManagerCount { get; set; }
        public int CustomerCount { get; set; }
        public string ManagerName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int PendingRefundCount { get; set; }
        public int PendingOrdersCount { get; set; }
        public List<CommentsDto> RecentComments { get; set; } = new();
        public List<OrderDto> RecentOrders { get; set; } = new();
        public List<ProductDto> LowStockProducts { get; set; } = new();
        public int TotalOrders { get; set; }

        public async Task OnGetAsync()
        {
            var comp = User.FindFirst(AppConstants.CustomClaimTypes.CompanyId)?.Value;
            if (!string.IsNullOrEmpty(comp) && int.TryParse(comp, out var companyId))
            {
                CompanyId = companyId;
            }
            else
            {
                var idClaim = User.FindFirst(AppConstants.CustomClaimTypes.UserId)?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var userId))
                {
                    var me = await _userService.GetByIdAsync(userId);
                    if (me != null)
                    {
                        CompanyId = me.CompanyId ?? 0;
                        ManagerName = me.FirstName + " " + me.LastName;
                    }
                }
            }
            if (CompanyId >= 0)
            {
                // 1. Fetch main dashboard data via Stored Procedure (Multiple Result Sets)
                // This replaces 4 separate database round-trips with 1.
                var dashboardData = await _dashboardService.GetDashboardDataAsync(CompanyId);
                
                WorkerCount = dashboardData.WorkerCount;
                ManagerCount = dashboardData.ManagerCount;
                CustomerCount = dashboardData.CustomerCount;
                PendingOrdersCount = dashboardData.PendingOrdersCount;
                RecentOrders = dashboardData.RecentOrders;
                LowStockProducts = dashboardData.LowStockProducts;

                // 2. Fetch remaining data sequentially
                ProductCount = await _productService.CountByCompanyAsync(CompanyId);
                PendingRefundCount = await _refundService.GetPendingRefundsCountAsync(CompanyId);
                CompanyName = (await _userService.GetCompanyNameAsync(CompanyId)) ?? string.Empty;
                
                var commentResult = await _commentService.GetCommentsForCompanyAsync(CompanyId, 1, 1, 3);
                RecentComments = commentResult.Items;

                // Note: Toplam siparişi eklemeyi unutma.
                var orderCounts = await _orderService.GetOrderCountsByCompanyIdAsync(CompanyId);
                TotalOrders = orderCounts.ActiveCount + orderCounts.CompletedCount + orderCounts.RefundedCount;
            }
        }
    }
}
