using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Core.Models;
using DomusMercatoris.Core.Enums;
using DomusMercatoris.Data;
using DomusMercatoris.Service.Interfaces;

namespace DomusMercatoris.Service.Services
{
    public class TaskService
    {
        private readonly DomusDbContext _db;
        private readonly ICurrentUserService _currentUserService;

        public TaskService(DomusDbContext db, ICurrentUserService currentUserService)
        {
            _db = db;
            _currentUserService = currentUserService;
        }

        public async Task<WorkTask> CreateTaskAsync(string title, string? description, long? orderId, long assignedToUserId, long createdByUserId, int companyId, long? parentId = null, TaskType type = TaskType.General)
        {
            var task = new WorkTask
            {
                Title = title,
                Type = type,
                Description = description,
                OrderId = orderId,
                AssignedToUserId = assignedToUserId,
                CreatedByUserId = createdByUserId,
                CompanyId = companyId,
                ParentId = parentId,
                CreatedAt = DateTime.UtcNow
            };

            _db.WorkTasks.Add(task);
            await _db.SaveChangesAsync();
            return task;
        }

        public async Task<bool> UpdateTaskStatusAsync(long taskId, bool isCompleted)
        {
            var task = await _db.WorkTasks.Include(t => t.Order).FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) return false;

            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null) return false;

            var isManager = _currentUserService.IsManager;
            var hasPermission = await _currentUserService.HasPermissionAsync("Tasks");

            // Permission check
            if (task.AssignedToUserId == currentUserId || isManager || hasPermission)
            {
                task.IsCompleted = isCompleted;
                task.CompletedAt = isCompleted ? DateTime.UtcNow : null;

                // Handle Side Effects / Workflow
                if (isCompleted)
                {
                    await ProcessTaskCompletionWorkflowAsync(task);
                }

                await _db.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> CompleteShippingTaskAsync(long taskId, string trackingNumber)
        {
            var task = await _db.WorkTasks
                .Include(t => t.Order)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return false;

            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null) return false;
            var isManager = _currentUserService.IsManager;

            // Permission Check
            if (!isManager && task.AssignedToUserId != currentUserId)
            {
                return false;
            }

            // Create CargoTracking
            var cargo = new CargoTracking
            {
                TrackingNumber = trackingNumber,
                CarrierName = "Kargo",
                Status = CargoStatus.InTransit,
                CreatedAt = DateTime.UtcNow,
                ShippedDate = DateTime.UtcNow,
                UserId = task.Order?.UserId != 0 ? task.Order?.UserId : null,
                FleetingUserId = task.Order?.FleetingUserId
            };
            _db.CargoTrackings.Add(cargo);

            // Update Order
            if (task.Order != null)
            {
                task.Order.Status = OrderStatus.Shipped; // 5
                task.Order.CargoTracking = cargo;
                _db.Entry(task.Order).State = EntityState.Modified;
            }

            // Complete Task
            task.IsCompleted = true;
            task.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        private Task ProcessTaskCompletionWorkflowAsync(WorkTask task)
        {
            if (task.Order == null) return Task.CompletedTask;

            // Handle Packaging -> Preparing
            if (task.Type == TaskType.Packaging)
            {
                task.Order.Status = OrderStatus.Preparing; // 4
                _db.Entry(task.Order).State = EntityState.Modified;
            }
            return Task.CompletedTask;
        }


        public async Task<List<WorkTask>> GetPendingTasksForUserAsync(long userId)
        {
            return await _db.WorkTasks
                .Include(t => t.Order)
                .Where(t => t.AssignedToUserId == userId && !t.IsCompleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<WorkTask>> GetManagedTasksAsync(int companyId, int limit = 50)
        {
            return await _db.WorkTasks
                .Include(t => t.Order)
                .Include(t => t.AssignedToUser)
                .Where(t => t.CompanyId == companyId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<WorkTask>> GetTasksByOrderIdsAsync(List<long> orderIds)
        {
            return await _db.WorkTasks
                .Include(t => t.AssignedToUser)
                .Where(t => t.OrderId.HasValue && orderIds.Contains(t.OrderId.Value))
                .ToListAsync();
        }

        public async Task<bool> DeleteTaskAsync(long taskId)
        {
            var task = await _db.WorkTasks.FindAsync(taskId);
            if (task == null) return false;

            _db.WorkTasks.Remove(task);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<WorkTask?> UpdateTaskAssignmentAsync(long taskId, string title, string? description, long assignedToUserId, long? orderId)
        {
            var task = await _db.WorkTasks.FindAsync(taskId);
            if (task == null) return null;

            task.Title = title;
            task.Description = description;
            task.AssignedToUserId = assignedToUserId;
            if (orderId.HasValue) task.OrderId = orderId;
            
            await _db.SaveChangesAsync();
            return task;
        }
    }
}
