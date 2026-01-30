using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Constants;

namespace DomusMercatorisDotnetMVC.Pages
{
    [Authorize(Policy = "OrdersAccess")]
    public class OrdersModel : PageModel
    {
        private readonly DomusMercatoris.Service.Services.OrderService _orderService;
        private readonly UserService _userService;
        private readonly TaskService _taskService;

        public OrdersModel(DomusMercatoris.Service.Services.OrderService orderService, UserService userService, TaskService taskService)
        {
            _orderService = orderService;
            _userService = userService;
            _taskService = taskService;
        }

        public List<Order> Orders { get; set; } = new List<Order>();
        public List<User> Workers { get; set; } = new();
        public Dictionary<long, WorkTask> OrderAssignedUsers { get; set; } = new();

        [BindProperty]
        public TaskInput NewTask { get; set; } = new();

        public class TaskInput
        {
            public long? Id { get; set; } // Added for Edit/Delete
            public long OrderId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public long AssignedToUserId { get; set; }
        }

        [BindProperty(SupportsGet = true, Name = "page")]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true, Name = "tab")]
        public string ActiveTab { get; set; } = "recent";

        public int PageSize { get; set; } = 5;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 1;
        public int ActiveOrdersCount { get; set; }
        public int CompletedOrdersCount { get; set; }
        public int RefundedOrdersCount { get; set; }
        public long CurrentUserId { get; set; } // Added for View access

        public async Task OnGetAsync()
        {
            int companyId = await _userService.GetCompanyIdFromUserAsync(User);
            Console.WriteLine($"[DEBUG] OnGetAsync: PageNumber property = {PageNumber}");
            Console.WriteLine($"[DEBUG] OnGetAsync: Query['page'] = {Request.Query["page"]}");

            if (companyId > 0)
            {
                var idClaim = User.FindFirst(AppConstants.CustomClaimTypes.UserId)?.Value;
                if (!string.IsNullOrEmpty(idClaim) && long.TryParse(idClaim, out var uid))
                {
                    CurrentUserId = uid;
                }

                if (Request.Query.ContainsKey("page") && int.TryParse(Request.Query["page"], out var p))
                {
                    PageNumber = p;
                }

                if (PageNumber < 1) PageNumber = 1;

                var counts = await _orderService.GetOrderCountsByCompanyIdAsync(companyId);
                ActiveOrdersCount = counts.ActiveCount;
                CompletedOrdersCount = counts.CompletedCount;
                RefundedOrdersCount = counts.RefundedCount;

                var result = await _orderService.GetPagedByCompanyIdAsync(companyId, PageNumber, PageSize, ActiveTab);
                
                Orders = result.Items;
                TotalCount = result.TotalCount;
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
                
                if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

                var orderIds = Orders.Select(o => o.Id).ToList();
                var tasks = await _taskService.GetTasksByOrderIdsAsync(orderIds);
                OrderAssignedUsers = tasks
                    .Where(t => t.OrderId.HasValue && !t.IsCompleted)
                    .GroupBy(t => t.OrderId!.Value)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.CreatedAt).First());

                var allUsers = await _userService.GetByCompanyAsync(companyId);
                Workers = allUsers
                    .Where(u => u.Id == CurrentUserId || !(u.Roles ?? new List<string>()).Any(r => 
                        string.Equals(r, AppConstants.Roles.Customer, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r, AppConstants.Roles.Rex, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
        }

        public async Task<IActionResult> OnPostAssignTaskAsync()
        {
            int companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId == 0) return RedirectToPage();

            if (NewTask.AssignedToUserId == 0)
            {
                return RedirectToPage();
            }

            long currentUserId = 0;
            var idClaim = User.FindFirst(AppConstants.CustomClaimTypes.UserId)?.Value;
            if (!string.IsNullOrEmpty(idClaim)) long.TryParse(idClaim, out currentUserId);

            if (NewTask.Id.HasValue && NewTask.Id.Value > 0)
            {
                // Update existing task - keep as is for flexibility or restrict? 
                // For now, let's assume reassignment just updates the user, but titles are fixed.
                // Actually, the requirement implies a flow. Let's just create the fixed workflow tasks if it's a new assignment.
                await _taskService.UpdateTaskAssignmentAsync(NewTask.Id.Value, NewTask.Title, NewTask.Description, NewTask.AssignedToUserId, NewTask.OrderId);
            }
            else
            {
                // Create Fixed Workflow Tasks
                // 1. Packaging
                await _taskService.CreateTaskAsync("Packaging", "Prepare and package the order items.", NewTask.OrderId, NewTask.AssignedToUserId, currentUserId, companyId);
                
                // 2. Shipping
                await _taskService.CreateTaskAsync("Shipping", "Ship the package and enter the tracking number.", NewTask.OrderId, NewTask.AssignedToUserId, currentUserId, companyId);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteTaskAsync(long taskId)
        {
             int companyId = await _userService.GetCompanyIdFromUserAsync(User);
             if (companyId == 0) return RedirectToPage();

             await _taskService.DeleteTaskAsync(taskId);
             return RedirectToPage();
        }
    }
}
