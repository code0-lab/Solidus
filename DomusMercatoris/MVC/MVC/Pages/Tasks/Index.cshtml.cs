using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Core.Models;
using DomusMercatoris.Core.Constants;

namespace DomusMercatorisDotnetMVC.Pages.Tasks
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly TaskService _taskService;
        private readonly UserService _userService;
        private readonly DomusDbContext _db;

        public IndexModel(TaskService taskService, UserService userService, DomusDbContext db)
        {
            _taskService = taskService;
            _userService = userService;
            _db = db;
        }

        public List<WorkTask> MyTasks { get; set; } = new();
        public List<WorkTask> ManagedTasks { get; set; } = new();
        public bool CanManageTasks { get; set; }
        public int CompanyId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId == 0) throw new ForbiddenException("Access denied.");
            CompanyId = companyId;

            long currentUserId = 0;
            var idClaim = User.FindFirst(AppConstants.CustomClaimTypes.UserId)?.Value;
            if (!string.IsNullOrEmpty(idClaim)) long.TryParse(idClaim, out currentUserId);

            if (currentUserId > 0)
            {
                // Check if user is banned
                var isBanned = await _db.Bans
                    .AsNoTracking()
                    .AnyAsync(b => b.UserId == currentUserId && (b.PermaBan || b.EndDate > DateTime.UtcNow));
                
                if (isBanned)
                {
                    return RedirectToPage("/Banned");
                }

                // My Pending Tasks
                MyTasks = await _taskService.GetPendingTasksForUserAsync(currentUserId);

                // Check Permissions
                bool isManager = User.IsInRole(AppConstants.Roles.Manager);
                bool hasTaskPermission = false;
                if (!isManager)
                {
                    hasTaskPermission = await _db.UserPageAccesses
                        .AnyAsync(p => p.UserId == currentUserId && p.PageKey == "Tasks");
                }

                CanManageTasks = isManager || hasTaskPermission;

                if (CanManageTasks)
                {
                    ManagedTasks = await _taskService.GetManagedTasksAsync(companyId);
                }
            }

            // Return Partial View if requested via AJAX
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Page();
            }

            return Page();
        }

        public class AssignTaskInput
        {
            public long OrderId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public long AssignedToUserId { get; set; }
        }

        [BindProperty]
        public AssignTaskInput NewTask { get; set; } = new();

        public async Task<IActionResult> OnPostAssignAsync()
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId == 0) throw new ForbiddenException("Access denied.");

            long currentUserId = 0;
            var idClaim = User.FindFirst(AppConstants.CustomClaimTypes.UserId)?.Value;
            if (!string.IsNullOrEmpty(idClaim)) long.TryParse(idClaim, out currentUserId);

            // Check if user is banned
            if (currentUserId > 0)
            {
                var isBanned = await _db.Bans
                    .AsNoTracking()
                    .AnyAsync(b => b.UserId == currentUserId && (b.PermaBan || b.EndDate > DateTime.UtcNow));
                
                if (isBanned) return RedirectToPage("/Banned");
            }

            if (NewTask.AssignedToUserId != 0 && !string.IsNullOrEmpty(NewTask.Title))
            {
                await _taskService.CreateTaskAsync(NewTask.Title, NewTask.Description, NewTask.OrderId, NewTask.AssignedToUserId, currentUserId, companyId);
            }

            return RedirectToPage("/Orders");
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(long taskId, bool isCompleted)
        {
            long currentUserId = 0;
            var idClaim = User.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(idClaim)) long.TryParse(idClaim, out currentUserId);

            bool isManager = User.IsInRole("Manager");
            bool hasPermission = false;
            
            if (!isManager)
            {
                hasPermission = await _db.UserPageAccesses
                    .AnyAsync(p => p.UserId == currentUserId && p.PageKey == "Tasks");
            }

            var task = await _db.WorkTasks.Include(t => t.Order).FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) throw new NotFoundException($"Task {taskId} not found.");

            // Permission check
            if (task.AssignedToUserId == currentUserId || isManager || hasPermission)
            {
                task.IsCompleted = isCompleted;
                task.CompletedAt = isCompleted ? DateTime.UtcNow : null;

                // Order Workflow Logic
                if (task.Order != null && isCompleted)
                {
                    if (task.Title == "Packaging")
                    {
                        task.Order.Status = OrderStatus.Preparing; // 4
                        _db.Entry(task.Order).State = EntityState.Modified;
                    }
                    // "Kargolama" is handled in OnPostCompleteTrackingAsync
                }

                await _db.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }

            throw new ForbiddenException("Access denied.");
        }

        public async Task<IActionResult> OnPostCompleteTrackingAsync(long taskId, string trackingNumber)
        {
            var companyId = await _userService.GetCompanyIdFromUserAsync(User);
            if (companyId == 0) throw new ForbiddenException("Access denied.");

            long currentUserId = 0;
            var idClaim = User.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(idClaim)) long.TryParse(idClaim, out currentUserId);

            // Fetch Task with Order
            var task = await _db.WorkTasks
                .Include(t => t.Order)
                .FirstOrDefaultAsync(t => t.Id == taskId && t.CompanyId == companyId);

            if (task == null) throw new NotFoundException($"Task {taskId} not found.");

            // Permission Check
            bool isManager = User.IsInRole("Manager");
            if (!isManager && task.AssignedToUserId != currentUserId)
            {
                throw new ForbiddenException("Access denied.");
            }

            if (string.IsNullOrEmpty(trackingNumber)) throw new BadRequestException("Tracking number is required.");

            // 1. Create CargoTracking
            var cargo = new CargoTracking
            {
                TrackingNumber = trackingNumber,
                CarrierName = "Cargo", // Default, as user only provides tracking no
                Status = DomusMercatoris.Core.Models.CargoStatus.InTransit,
                CreatedAt = DateTime.UtcNow,
                ShippedDate = DateTime.UtcNow,
                UserId = task.Order?.UserId != 0 ? task.Order?.UserId : null,
                FleetingUserId = task.Order?.FleetingUserId
            };
            
            _db.CargoTrackings.Add(cargo);
            await _db.SaveChangesAsync();

            // 2. Update Order
            if (task.Order != null)
            {
                task.Order.CargoTrackingId = cargo.Id;
                task.Order.Status = DomusMercatoris.Core.Models.OrderStatus.Shipped;
                _db.Entry(task.Order).State = EntityState.Modified;
            }

            // 3. Complete Task
            task.IsCompleted = true;
            task.CompletedAt = DateTime.UtcNow;
            _db.Entry(task).State = EntityState.Modified;

            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
    }
}
