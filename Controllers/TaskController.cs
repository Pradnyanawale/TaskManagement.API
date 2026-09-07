using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagement.API.Data;
using TaskManagement.API.DTOs.Task;
using TaskManagement.API.Models;
using TaskManagement.API.Services;
using TaskManagement.API.Services;


namespace TaskManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        public TasksController(
      AppDbContext context,
      INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }
     

        // ============================================================
        // CREATE TASK
        // Admin and Manager can create and assign tasks
        // ============================================================

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
       
        public async Task<IActionResult> CreateTask(CreateTaskDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            // Validate status
            if (dto.Status != "To Do" &&
                dto.Status != "In Progress" &&
                dto.Status != "Done")
            {
                return BadRequest(new
                {
                    message = "Invalid status. Allowed values: To Do, In Progress, Done"
                });
            }

            // Validate priority
            if (dto.Priority != "Low" &&
                dto.Priority != "Medium" &&
                dto.Priority != "High")
            {
                return BadRequest(new
                {
                    message = "Invalid priority. Allowed values: Low, Medium, High"
                });
            }

            // Check assigned user
            var assignedUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.AssignedToId);

            if (assignedUser == null)
            {
                return BadRequest(new
                {
                    message = "Assigned user not found"
                });
            }

            // Manager can assign only to their team members
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == "Manager")
            {
                bool isTeamMember = await _context.TeamMembers
                    .AnyAsync(tm =>
                        tm.UserId == dto.AssignedToId &&
                        tm.Team!.ManagerId == userId);

                if (!isTeamMember)
                {
                    return Forbid();
                }
            }

            // Create task
            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status,
                Priority = dto.Priority,
                Deadline = dto.Deadline,
                AssignedToId = dto.AssignedToId,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);

            await _context.SaveChangesAsync();

            // Create notification for assigned user
            await _notificationService.CreateNotificationAsync(
                task.AssignedToId,
                $"You have been assigned a new task: {task.Title}"
            );

            return Ok(new
            {
                message = "Task created successfully",
                taskId = task.Id,
                createdById = task.CreatedById,
                assignedToId = task.AssignedToId
            });
        }

        // ============================================================
        // GET ALL / ASSIGNED TASKS
        // Admin + Manager -> relevant tasks
        // User -> assigned tasks only
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetTasks(
    string? status,
    string? priority,
    DateTime? deadline)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            IQueryable<TaskItem> query = _context.Tasks;

            // ============================================================
            // ROLE-BASED TASK ACCESS
            // ============================================================

            if (role == "Admin")
            {
                // Admin can see all tasks
            }
            else if (role == "Manager")
            {
                // Manager can see tasks created by them
                // or tasks assigned to members of their teams
                query = query.Where(t =>
                    t.CreatedById == userId ||
                    _context.TeamMembers.Any(tm =>
                        tm.UserId == t.AssignedToId &&
                        tm.Team!.ManagerId == userId));
            }
            else
            {
                // User can see only tasks assigned to them
                query = query.Where(t => t.AssignedToId == userId);
            }

            // ============================================================
            // STATUS FILTER
            // ============================================================

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status != "To Do" &&
                    status != "In Progress" &&
                    status != "Done")
                {
                    return BadRequest(new
                    {
                        message = "Invalid status. Allowed values: To Do, In Progress, Done"
                    });
                }

                query = query.Where(t => t.Status == status);
            }

            // ============================================================
            // PRIORITY FILTER
            // ============================================================

            if (!string.IsNullOrWhiteSpace(priority))
            {
                if (priority != "Low" &&
                    priority != "Medium" &&
                    priority != "High")
                {
                    return BadRequest(new
                    {
                        message = "Invalid priority. Allowed values: Low, Medium, High"
                    });
                }

                query = query.Where(t => t.Priority == priority);
            }

            // ============================================================
            // DEADLINE FILTER
            // ============================================================

            if (deadline.HasValue)
            {
                var selectedDate = deadline.Value.Date;
                var nextDate = selectedDate.AddDays(1);

                query = query.Where(t =>
                    t.Deadline >= selectedDate &&
                    t.Deadline < nextDate);
            }

            // ============================================================
            // RETURN TASKS
            // ============================================================

            var tasks = await query
                .OrderBy(t => t.Deadline)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Status,
                    t.Priority,
                    t.Deadline,
                    t.AssignedToId,
                    t.CreatedById,
                    t.CreatedAt
                })
                .ToListAsync();

            return Ok(tasks);
        }


        // ============================================================
        // GET TASK BY ID
        // ============================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTask(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound(new
                {
                    message = "Task not found"
                });
            }

            // Admin can see everything
            if (role == "Admin")
            {
                return Ok(task);
            }

            // Manager can see tasks they created
            // or tasks assigned to their team members
            if (role == "Manager")
            {
                bool allowed = task.CreatedById == userId ||
                               await _context.TeamMembers.AnyAsync(tm =>
                                   tm.UserId == task.AssignedToId &&
                                   tm.Team!.ManagerId == userId);

                if (!allowed)
                {
                    return Forbid();
                }

                return Ok(task);
            }

            // User can see only tasks assigned to them
            if (task.AssignedToId != userId)
            {
                return Forbid();
            }

            return Ok(task);
        }


        // ============================================================
        // UPDATE TASK
        //
        // Admin -> can update everything
        // Manager -> can update everything for their team's tasks
        // User -> can update STATUS only
        // ============================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(
            int id,
            UpdateTaskDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound(new
                {
                    message = "Task not found"
                });
            }

            // --------------------------------------------------------
            // USER
            // --------------------------------------------------------

            // --------------------------------------------------------
            // USER
            // --------------------------------------------------------

            // --------------------------------------------------------
            // USER
            // --------------------------------------------------------

            if (role == "User")
            {
                // User can update only assigned tasks
                if (task.AssignedToId != userId)
                {
                    return Forbid();
                }

                // Validate status
                if (dto.Status != "To Do" &&
                    dto.Status != "In Progress" &&
                    dto.Status != "Done")
                {
                    return BadRequest(new
                    {
                        message = "Invalid status. Allowed values: To Do, In Progress, Done"
                    });
                }

                // Store old status before changing it
                var oldStatus = task.Status;

                // Update status
                task.Status = dto.Status;

                await _context.SaveChangesAsync();

                // Create notification only when status actually changes
                if (oldStatus != task.Status)
                {
                    await _notificationService.CreateNotificationAsync(
                        task.CreatedById,
                        $"Task '{task.Title}' status changed from {oldStatus} to {task.Status}"
                    );
                }

                return Ok(new
                {
                    message = "Task status updated successfully",
                    taskId = task.Id,
                    status = task.Status
                });
            }

            // --------------------------------------------------------
            // MANAGER
            // --------------------------------------------------------

            if (role == "Manager")
            {
                bool allowed = task.CreatedById == userId ||
                               await _context.TeamMembers.AnyAsync(tm =>
                                   tm.UserId == task.AssignedToId &&
                                   tm.Team!.ManagerId == userId);

                if (!allowed)
                {
                    return Forbid();
                }
            }

            // --------------------------------------------------------
            // ADMIN / MANAGER
            // --------------------------------------------------------

            if (role != "Admin" && role != "Manager")
            {
                return Forbid();
            }

            // Validate status
            if (dto.Status != "To Do" &&
                dto.Status != "In Progress" &&
                dto.Status != "Done")
            {
                return BadRequest(new
                {
                    message = "Invalid status. Allowed values: To Do, In Progress, Done"
                });
            }

            // Validate priority
            if (dto.Priority != "Low" &&
                dto.Priority != "Medium" &&
                dto.Priority != "High")
            {
                return BadRequest(new
                {
                    message = "Invalid priority. Allowed values: Low, Medium, High"
                });
            }

            // Validate assigned user
            var assignedUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.AssignedToId);

            if (assignedUser == null)
            {
                return BadRequest(new
                {
                    message = "Assigned user not found"
                });
            }

            // Manager can assign only to their team members
            if (role == "Manager")
            {
                bool isTeamMember = await _context.TeamMembers
                    .AnyAsync(tm =>
                        tm.UserId == dto.AssignedToId &&
                        tm.Team!.ManagerId == userId);

                if (!isTeamMember)
                {
                    return Forbid();
                }
            }

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Status = dto.Status;
            task.Priority = dto.Priority;
            task.Deadline = dto.Deadline;
            task.AssignedToId = dto.AssignedToId;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Task updated successfully",
                taskId = task.Id
            });
        }


        // ============================================================
        // DELETE TASK
        // Admin only
        // ============================================================

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound(new
                {
                    message = "Task not found"
                });
            }

            _context.Tasks.Remove(task);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Task deleted successfully",
                taskId = id
            });
        }
    }
}