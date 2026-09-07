using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaskManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetMe()
        {
            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Email = User.FindFirst(ClaimTypes.Name)?.Value,
                Role = User.FindFirst(ClaimTypes.Role)?.Value
            });
        }
    }
}