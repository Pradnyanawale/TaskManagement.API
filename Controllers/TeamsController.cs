using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagement.API.Data;
using TaskManagement.API.DTO.Team;
using TaskManagement.API.Models;

namespace TaskManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TeamsController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // CREATE TEAM
        // POST: api/Teams
        // Admin + Manager
        // ==========================================
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> CreateTeam(
            CreateTeamDto dto)
        {
            var existingTeam = await _context.Teams
                .FirstOrDefaultAsync(t => t.Name == dto.Name);

            if (existingTeam != null)
            {
                return BadRequest(new
                {
                    message = "Team already exists"
                });
            }

            // Check Manager exists
            var manager = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == dto.ManagerId &&
                    u.Role == "Manager");

            if (manager == null)
            {
                return BadRequest(new
                {
                    message = "Manager not found"
                });
            }

            var team = new Team
            {
                Name = dto.Name,
                ManagerId = dto.ManagerId
            };

            _context.Teams.Add(team);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Team created successfully",
                teamId = team.Id,
                name = team.Name,
                managerId = team.ManagerId
            });
        }


        // ==========================================
        // GET ALL TEAMS
        // GET: api/Teams
        // All authenticated users
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetTeams()
        {
            var teams = await _context.Teams
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.ManagerId
                })
                .ToListAsync();

            return Ok(teams);
        }


        // ==========================================
        // ADD USER TO TEAM
        // POST: api/Teams/{teamId}/members/{userId}
        // Admin + Manager
        // ==========================================
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("{teamId}/members/{userId}")]
        public async Task<IActionResult> AddMember(
            int teamId,
            int userId)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
            {
                return NotFound(new
                {
                    message = "Team not found"
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found"
                });
            }

            // Check if user already belongs to team
            var existingMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm =>
                    tm.TeamId == teamId &&
                    tm.UserId == userId);

            if (existingMember != null)
            {
                return BadRequest(new
                {
                    message = "User is already a member of this team"
                });
            }

            var teamMember = new TeamMember
            {
                TeamId = teamId,
                UserId = userId
            };

            _context.TeamMembers.Add(teamMember);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User added to team successfully",
                teamId = teamId,
                userId = userId
            });
        }


        // ==========================================
        // GET TEAM MEMBERS
        // GET: api/Teams/{teamId}/members
        // All authenticated users
        // ==========================================
        [HttpGet("{teamId}/members")]
        public async Task<IActionResult> GetTeamMembers(
            int teamId)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
            {
                return NotFound(new
                {
                    message = "Team not found"
                });
            }

            var members = await _context.TeamMembers
                .Where(tm => tm.TeamId == teamId)
                .Select(tm => new
                {
                    tm.UserId,
                    Name = tm.User!.Name,
                    Email = tm.User.Email,
                    Role = tm.User.Role
                })
                .ToListAsync();

            return Ok(members);
        }


        // ==========================================
        // REMOVE USER FROM TEAM
        // DELETE: api/Teams/{teamId}/members/{userId}
        // Admin + Manager
        // ==========================================
        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{teamId}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(
            int teamId,
            int userId)
        {
            var member = await _context.TeamMembers
                .FirstOrDefaultAsync(tm =>
                    tm.TeamId == teamId &&
                    tm.UserId == userId);

            if (member == null)
            {
                return NotFound(new
                {
                    message = "Team member not found"
                });
            }

            _context.TeamMembers.Remove(member);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User removed from team successfully",
                teamId = teamId,
                userId = userId
            });
        }
    }
}