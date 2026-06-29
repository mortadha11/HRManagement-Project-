using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRManagement.API.Application.DTOs;
using HRManagement.API.Application.Interfaces;
using HRManagement.API.Domain.Entities;
using HRManagement.API.Infrastructure.Data;

namespace HRManagement.API.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly AppDbContext   _context;
    private readonly ITokenService  _tokenService;
    private readonly IEmailService  _emailService;

    public AuthController(AppDbContext context, ITokenService tokenService, IEmailService emailService)
    {
        _context      = context;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    // ── Login ─────────────────────────────────────────────
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

        return Ok(new
        {
            token      = _tokenService.GenerateToken(user),
            userId     = user.Id,
            username   = user.Username,
            role       = user.Role,
            employeeId = user.EmployeeId,
            fullName   = user.Employee != null
                ? $"{user.Employee.FirstName} {user.Employee.LastName}"
                : ""
        });
    }

    // ── Forgot Password (self-service, no admin needed) ───
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { message = "Username is required." });

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        // Always 200 — don't reveal whether the username exists
        if (user == null)
            return Ok(new
            {
                message     = "If this username exists, a temporary password has been generated.",
                tempPassword = (string?)null
            });

        var tempPassword      = GenerateTempPassword();
        user.PasswordHash     = BCrypt.Net.BCrypt.HashPassword(tempPassword);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message      = "Temporary password generated. Log in and change it immediately.",
            tempPassword
        });
    }

    // ── Current user info ─────────────────────────────────
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
            e.Id, e.FirstName, e.LastName, e.Email, e.Phone,
            e.HireDate, e.Salary, e.JobTitle, e.JobLevel, e.IsActive,
            DepartmentName = e.Department?.Name,
            ManagerName    = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}" : null,
            role           = e.User?.Role ?? "Employee",
            username       = e.User?.Username
        });
    }

    // ── Get user account by employee ──────────────────────
    [HttpGet("user/{employeeId:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetUserByEmployee(int employeeId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user == null) return NotFound(new { message = "No account found for this employee." });

        return Ok(new
        {
            user.Id, user.Username, user.Role, user.IsActive,
            user.CreatedAt, user.LastLoginAt,
            hasPassword = !string.IsNullOrEmpty(user.PasswordHash)
        });
    }

    // ── Admin: reset password ─────────────────────────────
    [HttpPost("reset-password")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == request.EmployeeId);
        if (user == null) return NotFound(new { message = "No account found for this employee." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Password reset for {user.Username}." });
    }

    // ── Admin: create account ─────────────────────────────
    [HttpPost("create-account")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return Conflict(new { message = "Username already exists." });

        if (await _context.Users.AnyAsync(u => u.EmployeeId == request.EmployeeId))
            return Conflict(new { message = "This employee already has an account." });

        var user = new User
        {
            EmployeeId   = request.EmployeeId,
            Username     = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role         = request.Role,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        Console.WriteLine($"[DEBUG] AuthController.CreateAccount called for EmployeeId {request.EmployeeId}");
        var employeeInfo = await _context.Employees.Where(e => e.Id == request.EmployeeId).Select(e => new { e.FirstName, e.LastName, e.Email }).FirstOrDefaultAsync();
        
        if (employeeInfo != null && !string.IsNullOrWhiteSpace(employeeInfo.Email))
        {
            Console.WriteLine($"[DEBUG] Employee found: {employeeInfo.Email}");
            _ = _emailService.SendWelcomeEmailAsync(
                toEmail: employeeInfo.Email,
                toName: $"{employeeInfo.FirstName} {employeeInfo.LastName}",
                username: request.Username,
                temporaryPassword: request.Password
            );
        }
        else 
        {
            Console.WriteLine("[DEBUG] Employee not found or missing email!");
        }

        return Ok(new { message = $"Account created: {user.Username} ({user.Role})" });
    }

    // ── Self: change password ─────────────────────────────
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Both passwords are required." });

        if (request.NewPassword.Length < 6)
            return BadRequest(new { message = "New password must be at least 6 characters." });

        var employeeIdClaim = User.FindFirst("employeeId")?.Value;
        if (!int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized(new { message = "Invalid session. Please sign in again." });

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId && u.IsActive);
        if (user == null) return NotFound(new { message = "Account not found." });

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return Unauthorized(new { message = "Current password is incorrect." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Password updated successfully." });
    }

    // ── Helpers ───────────────────────────────────────────
    private bool CanAccessEmployee(int employeeId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Manager")) return true;
        var claim = User.FindFirst("employeeId")?.Value;
        return int.TryParse(claim, out var current) && current == employeeId;
    }

    private static string GenerateTempPassword(int length = 12)
    {
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower   = "abcdefghijkmnpqrstuvwxyz";
        const string digits  = "23456789";
        const string symbols = "!@#$%";
        var all  = upper + lower + digits + symbols;
        var rng  = new Random();
        var chars = new char[length];
        chars[0] = upper[rng.Next(upper.Length)];
        chars[1] = lower[rng.Next(lower.Length)];
        chars[2] = digits[rng.Next(digits.Length)];
        chars[3] = symbols[rng.Next(symbols.Length)];
        for (int i = 4; i < length; i++) chars[i] = all[rng.Next(all.Length)];
        return new string(chars.OrderBy(_ => rng.Next()).ToArray());
    }
}
