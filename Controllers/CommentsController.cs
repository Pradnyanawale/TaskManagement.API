
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagement.API.Data;
using TaskManagement.API.DTOs.Comment;
using TaskManagement.API.Models;

namespace TaskManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // CREATE COMMENT
        // POST: api/Comments
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> CreateComment(
            CreateCommentDto dto)
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim);

            // Check whether task exists
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == dto.TaskItemId);

            if (task == null)
            {
                return NotFound(new
                {
                    message = "Task not found"
                });
            }

            var comment = new Comment
            {
                Content = dto.Content,
                TaskItemId = dto.TaskItemId,

                // Get user from JWT
                UserId = userId,

                // Set by server
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Comment created successfully",
                commentId = comment.Id,
                taskId = comment.TaskItemId,
                userId = comment.UserId,
                createdAt = comment.CreatedAt
            });
        }


        // =========================================================
        // GET COMMENTS FOR TASK
        // GET: api/Comments/task/{taskId}
        // =========================================================
        [HttpGet("task/{taskId}")]
        public async Task<IActionResult> GetComments(int taskId)
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var role =
                User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim);

            // Check task exists
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                return NotFound(new
                {
                    message = "Task not found"
                });
            }

            // Admin and Manager can view all task comments
            if (role != "Admin" && role != "Manager")
            {
                // User can view comments only for
                // tasks created by them or assigned to them
                if (task.CreatedById != userId &&
                    task.AssignedToId != userId)
                {
                    return Forbid();
                }
            }

            // Return only required fields
            var comments = await _context.Comments
                .Where(c => c.TaskItemId == taskId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Content,
                    c.TaskItemId,
                    c.UserId,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(comments);
        }


        // =========================================================
        // DELETE COMMENT
        // DELETE: api/Comments/{id}
        // Admin + Manager only
        // =========================================================
        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                return NotFound(new
                {
                    message = "Comment not found"
                });
            }

            _context.Comments.Remove(comment);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Comment deleted successfully",
                commentId = id
            });
        }
    }
}

