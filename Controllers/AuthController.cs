using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PlanAI.Data;
using PlanAI.Models;

namespace PlanAI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                    return BadRequest("Email and password are required.");

                var role = string.IsNullOrWhiteSpace(req.Role) ? "TeamMember" : req.Role;
                if (role != "ProjectManager" && role != "TeamMember")
                    return BadRequest("Role must be 'ProjectManager' or 'TeamMember'.");

                var existing = _db.Users.FirstOrDefault(u => u.Email == req.Email);

                if (existing != null)
                    return Conflict("User with that email already exists.");

                var user = new AppUser
                {
                    Name = req.Name,
                    Email = req.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                    Role = role
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                return StatusCode(201, new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role,
                    user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"REGISTER ERROR: {ex.Message}\n{ex.InnerException?.Message}");
                return StatusCode(500, new { error = "Registration failed.", detail = ex.Message });
            }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            try
            {
                var user = _db.Users.FirstOrDefault(u => u.Email == req.Email);

                if (user == null)
                    return Unauthorized(new { error = "Invalid email or password." });

                if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                    return Unauthorized(new { error = "Invalid email or password." });

                var secret = _config["Auth:JwtSecret"] ?? "planai-super-secret-key-32-chars-minimum";
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var expiryHours = int.TryParse(_config["Auth:JwtExpiryHours"], out var h) ? h : 8;

                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(expiryHours),
                    signingCredentials: creds
                );

                var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

                Console.WriteLine($"LOGIN SUCCESS: User={user.Email}");

                return Ok(new
                {
                    token = tokenStr,
                    expires = token.ValidTo
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LOGIN ERROR: {ex.Message}\n{ex.InnerException?.Message}");
                return StatusCode(500, new { error = "Login failed.", detail = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("User ID not found in token claims.");

            if (!Guid.TryParse(userId, out var userGuid))
                return Unauthorized("Invalid User ID format in token.");

            var user = _db.Users.FirstOrDefault(u => u.Id == userGuid);

            if (user == null)
                return Unauthorized("User not found in database.");

            return Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.CreatedAt
            });
        }

        ///////////////////////////////////////////////////////////////
        // LIST USERS (For Managers to find assignees)
        ///////////////////////////////////////////////////////////////

        [Authorize(Roles = "ProjectManager,TeamMember")]
        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var users = _db.Users
                .Select(u => new { u.Id, u.Name, u.Email, u.Role })
                .ToList();

            return Ok(users);
        }

        public class RegisterRequest
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
        }

        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }
}
