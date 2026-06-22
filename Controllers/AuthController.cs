using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRManagement.API.Data;
using HRManagement.API.Services;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username and password are required." });

        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials." });

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);

        return Ok(new
        {
            token,
            userId = user.Id,
            username = user.Username,
            role = user.Role,
            employeeId = user.EmployeeId,
            fullName = user.Employee != null ? $"{user.Employee.FirstName} {user.Employee.LastName}" : ""
        });
    }

    [HttpGet("me/{employeeId:int}")]
    public async Task<IActionResult> GetMe(int employeeId)
    {
        if (!CanAccessEmployee(employeeId)) return Forbid();

        var e = await _context.Employees
            .Include(x => x.Department)
            .Include(x => x.Manager)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == employeeId);

        if (e == null) return NotFound();

        return Ok(new
        {
            e.Id,
            e.FirstName,
            e.LastName,
            e.Email,
            e.Phone,
            e.HireDate,
            e.Salary,
            e.JobTitle,
            e.JobLevel,
            e.IsActive,
            DepartmentName = e.Department?.Name,
            ManagerName = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}" : null,
            role = e.User?.Role ?? "Employee",
            username = e.User?.Username
        });
    }

    [HttpGet("user/{employeeId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetUserByEmployee(int employeeId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user == null) return NotFound(new { message = "No account found for this employee." });

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Role,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt,
            hasPassword = !string.IsNullOrEmpty(user.PasswordHash)
        });
    }

    [HttpPost("reset-password")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(new { message = "The password must contain at least 6 characters." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == request.EmployeeId);
        if (user == null) return NotFound(new { message = "No account found for this employee." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Password reset successfully for {user.Username}." });
    }

    [HttpPost("create-account")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return Conflict(new { message = "This username already exists." });

        if (await _context.Users.AnyAsync(u => u.EmployeeId == request.EmployeeId))
            return Conflict(new { message = "This employee already has an account." });

        var user = new HRManagement.API.Models.User
        {
            EmployeeId = request.EmployeeId,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Account created successfully: {user.Username} ({user.Role})" });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Current password and new password are required." });

        if (request.NewPassword.Length < 6)
            return BadRequest(new { message = "The new password must contain at least 6 characters." });

        var employeeIdClaim = User.FindFirst("employeeId")?.Value;
        if (!int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized(new { message = "Session is invalid. Please sign in again." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId && u.IsActive);
        if (user == null)
            return NotFound(new { message = "No account found for this employee." });

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return Unauthorized(new { message = "Current password is incorrect." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Password updated successfully." });
    }

    private bool CanAccessEmployee(int employeeId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Manager")) return true;
        var claim = User.FindFirst("employeeId")?.Value;
        return int.TryParse(claim, out var currentEmployeeId) && currentEmployeeId == employeeId;
    }
}

public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ResetPasswordRequest
{
    public int EmployeeId { get; set; }
    public string NewPassword { get; set; } = "";
}

public class CreateAccountRequest
{
    public int EmployeeId { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "Employee";
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
