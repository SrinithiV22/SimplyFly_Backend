using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SimplyFly.Api.Models;
using SimplyFly.Api.DTOs;
using SimplyFly.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace SimplyFly.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly PasswordHasher<User> _hasher = new();

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { message = "Name is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "Email is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { message = "Password is required" });
                }

                if (request.Password.Length < 6)
                {
                    return BadRequest(new { message = "Password must be at least 6 characters long" });
                }

                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
                
                if (existingUser != null)
                {
                    return BadRequest(new { message = "User with this email already exists" });
                }

                // Create new user
                var user = new User
                {
                    Name = request.Name.Trim(),
                    Email = request.Email.ToLower().Trim(),
                    Role = "User" // Default role
                };

                // Hash the password
                user.PasswordHash = _hasher.HashPassword(user, request.Password);

                // Add user to database
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Generate token for the new user
                var token = GenerateJwt(user);

                return Ok(new { 
                    token, 
                    user = new { 
                        user.Id, 
                        user.Name, 
                        user.Email, 
                        user.Role 
                    },
                    message = "Registration successful"
                });
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                Console.WriteLine($"Registration error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new { 
                    message = "Registration failed", 
                    error = ex.Message,
                    details = ex.InnerException?.Message 
                });
            }
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == dto.Email.ToLower());
                if (user == null)
                    return Unauthorized(new { message = "Invalid email or password." });

                var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
                if (result != PasswordVerificationResult.Success)
                    return Unauthorized(new { message = "Invalid email or password." });

                var token = GenerateJwt(user);
                return Ok(new { token, message = "Login successful" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return StatusCode(500, new { message = "Login failed", error = ex.Message });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;

            return Ok(new
            {
                Id = userId,
                Name = name,
                Email = email,
                Role = role
            });
        }

// Update the GetAllUsers method:

[HttpGet("users")]
[Authorize(Roles = "Admin,Flightowner")]
public async Task<IActionResult> GetAllUsers()
{
    try
    {
        var users = await _context.Users
            .Select(u => new
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                PasswordHash = "***Hidden***" // Consistent naming
            })
            .ToListAsync();
        
        Console.WriteLine($"Retrieved {users.Count} users from database");
        
        return Ok(users);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Get users error: {ex.Message}");
        return StatusCode(500, new { message = "Error retrieving users", error = ex.Message });
    }
}

        // DELETE: api/auth/user/{id} - Delete user (Admin only)
        [HttpDelete("user/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Don't allow admin to delete themselves
                var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (currentUserEmail == user.Email)
                {
                    return BadRequest(new { message = "Cannot delete your own account" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete user error: {ex.Message}");
                return StatusCode(500, new { message = "Error deleting user", error = ex.Message });
            }
        }

        // PUT: api/auth/user/{id} - Update user (Admin only)
        [HttpPut("user/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Update user properties
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    user.Name = request.Name.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    // Check if email is already taken by another user
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != id);
                    
                    if (existingUser != null)
                    {
                        return BadRequest(new { message = "Email is already taken by another user" });
                    }

                    user.Email = request.Email.ToLower().Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.Role))
                {
                    user.Role = request.Role;
                }

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    if (request.Password.Length < 6)
                    {
                        return BadRequest(new { message = "Password must be at least 6 characters long" });
                    }
                    user.PasswordHash = _hasher.HashPassword(user, request.Password);
                }

                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "User updated successfully",
                    user = new {
                        user.Id,
                        user.Name,
                        user.Email,
                        user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update user error: {ex.Message}");
                return StatusCode(500, new { message = "Error updating user", error = ex.Message });
            }
        }

        // GET: api/auth/user/{id} - Get user by ID (Admin only)
        [HttpGet("user/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id)
                    .Select(u => new
                    {
                        u.Id,
                        u.Name,
                        u.Email,
                        u.Role
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get user by ID error: {ex.Message}");
                return StatusCode(500, new { message = "Error retrieving user", error = ex.Message });
            }
        }

        private string GenerateJwt(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Name, user.Name)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddHours(2),
                claims: claims,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // DTO for updating users
    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Password { get; set; }
    }
}