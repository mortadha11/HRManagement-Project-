using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HRManagement.API.Data;
using HRManagement.API.Services;

namespace HRManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context      = context;
            _tokenService = tokenService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                return BadRequest(new { message = "Username et password obligatoires" });

            var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive == true);

            // Debug log — remove after testing
            if (user != null)
            {
                var isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
                Console.WriteLine($"[AUTH] User: {user.Username} | HashStart: {user.PasswordHash[..10]} | Valid: {isValid} | InputPwd: {request.Password}");
            }
            else
            {
                Console.WriteLine($"[AUTH] User not found: '{request.Username}'");
            }

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized(new { message = "Identifiants incorrects" });

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateToken(user);

            return Ok(new
            {
                message    = "Connexion réussie",
                token,
                userId     = user.Id,
                username   = user.Username,
                role       = user.Role,
                employeeId = user.EmployeeId,
                fullName   = user.Employee != null
                    ? $"{user.Employee.FirstName} {user.Employee.LastName}" : ""
            });
        }

        [HttpGet("me/{employeeId}")]
        [Authorize]
        public async Task<IActionResult> GetMe(int employeeId)
        {
            var e = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (e == null) return NotFound();

            return Ok(new {
                e.Id, e.FirstName, e.LastName,
                e.Email, e.Phone, e.HireDate, e.Salary,
                e.JobTitle, e.JobLevel, e.IsActive,
                DepartmentName = e.Department?.Name,
                ManagerName    = e.Manager != null
                    ? $"{e.Manager.FirstName} {e.Manager.LastName}" : null,
                role = e.User?.Role ?? "Employee"
            });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}