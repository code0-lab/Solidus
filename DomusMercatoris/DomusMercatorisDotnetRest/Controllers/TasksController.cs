using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Service.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Service.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DomusMercatorisDotnetRest.Controllers
{
    /// <summary>
    /// Manages worker tasks (Packaging, Shipping) and order workflow steps.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly TaskService _taskService;

        public TasksController(TaskService taskService)
        {
            _taskService = taskService;
        }

        /// <summary>
        /// Gets all pending tasks assigned to the authenticated user.
        /// </summary>
        /// <returns>List of pending tasks.</returns>
        [HttpGet("my-pending")]
        [ProducesResponseType(typeof(List<WorkTask>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyPendingTasks()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var tasks = await _taskService.GetPendingTasksForUserAsync(userId);
            return Ok(tasks);
        }

        /// <summary>
        /// Creates a new task (Manager only).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Manager")]
        [ProducesResponseType(typeof(WorkTask), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            if (companyId == 0) return BadRequest("Company ID not found.");

            var task = await _taskService.CreateTaskAsync(dto.Title, dto.Description, dto.OrderId, dto.AssignedToUserId, userId, companyId, dto.ParentId);
            return CreatedAtAction(nameof(GetMyPendingTasks), new { id = task.Id }, task);
        }

        /// <summary>
        /// Updates task assignment (Manager only).
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateTask(long id, [FromBody] UpdateTaskDto dto)
        {
            var updatedTask = await _taskService.UpdateTaskAssignmentAsync(id, dto.Title, dto.Description, dto.AssignedToUserId, dto.OrderId);
            if (updatedTask == null) return NotFound();
            return Ok(updatedTask);
        }

        /// <summary>
        /// Deletes a task (Manager only).
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteTask(long id)
        {
            var success = await _taskService.DeleteTaskAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Gets all tasks for the company (Manager only).
        /// </summary>
        [HttpGet("managed")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetManagedTasks([FromQuery] int limit = 50)
        {
            var companyId = GetCompanyId();
            if (companyId == 0) return BadRequest("Company ID not found.");

            var tasks = await _taskService.GetManagedTasksAsync(companyId, limit);
            return Ok(tasks);
        }

        /// <summary>
        /// Marks a task as completed (e.g. Packaging).
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/complete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteTask(long id)
        {
            var success = await _taskService.UpdateTaskStatusAsync(id, true);
            
            if (!success) return BadRequest("Could not complete task. Check permissions or task status.");
            
            return Ok(new { success = true });
        }

        /// <summary>
        /// Completes a shipping task by providing a tracking number.
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <param name="request">Tracking number payload</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/complete-tracking")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteTracking(long id, [FromBody] TrackingRequest request)
        {
            if (string.IsNullOrEmpty(request.TrackingNumber)) return BadRequest("Tracking number is required.");

            var success = await _taskService.CompleteShippingTaskAsync(id, request.TrackingNumber);

            if (!success) return BadRequest("Could not complete shipping task.");
            
            return Ok(new { success = true });
        }

        private long GetUserId()
        {
             var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
             if (long.TryParse(idClaim, out var id)) return id;
             // Try "UserId" claim as fallback
             var userIdClaim = User.FindFirst("UserId")?.Value;
             if (long.TryParse(userIdClaim, out var uid)) return uid;
             return 0;
        }

        private int GetCompanyId()
        {
             var claim = User.FindFirst("CompanyId")?.Value;
             if (int.TryParse(claim, out var id)) return id;
             return 0;
        }
    }

    public class TrackingRequest
    {
        public string TrackingNumber { get; set; } = string.Empty;
    }
}
