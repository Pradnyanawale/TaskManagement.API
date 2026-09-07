using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManagement.API.Data;
using TaskManagement.API.DTO.Auth;

using TaskManagement.API.Models;

namespace TaskManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // =========================
        // REGISTER
        // POST: api/Auth/register
        // =========================
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto request)
        {
            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return BadRequest(new
                {
                    message = "Email already registered"
                });
            }

            // Hash password using BCrypt
            string passwordHash =
                BCrypt.Net.BCrypt.HashPassword(request.Password);

            // New users are registered as User
            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = "User"
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User registered successfully",
                userId = user.Id,
                name = user.Name,
                email = user.Email,
                role = user.Role
            });
        }


        // =========================
        // LOGIN
        // POST: api/Auth/login
        // =========================
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password"
                });
            }

            // Verify BCrypt password
            bool passwordValid =
                BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    user.PasswordHash);

            if (!passwordValid)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password"
                });
            }

            // Create JWT
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login successful",
                token = token,
                userId = user.Id,
                name = user.Name,
                email = user.Email,
                role = user.Role
            });
        }


        // =========================
        // GENERATE JWT
        // =========================
        private string GenerateJwtToken(User user)
        {
            var jwtKey =
                _configuration["Jwt:Key"];

            var jwtIssuer =
                _configuration["Jwt:Issuer"];

            var jwtAudience =
                _configuration["Jwt:Audience"];

            var jwtExpiryMinutes =
                int.Parse(
                    _configuration["Jwt:ExpiryMinutes"] ?? "60");

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.Email),

                new Claim(
                    ClaimTypes.Role,
                    user.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey!));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    jwtExpiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}