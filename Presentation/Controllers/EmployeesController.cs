using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRManagement.API.Application.DTOs;
using HRManagement.API.Application.Interfaces;
using HRManagement.API.Domain.Entities;
using HRManagement.API.Infrastructure.Data;

namespace HRManagement.API.Presentation.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private static readonly string[] AllowedRoles = ["Employee", "Manager", "Admin"];

    private readonly AppDbContext                _context;
    private readonly ICredentialGeneratorService _credentials;
    private readonly IEmailService               _email;

    public EmployeesController(
        AppDbContext context,
        ICredentialGeneratorService credentials,
        IEmailService email)
    {
        _context     = context;
        _credentials = credentials;
        _email       = email;
    }

    // ── GET all active employees ───────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Include(e => e.User)
            .Where(e => e.IsActive)
            .Select(e => new
            {
                e.Id, e.FirstName, e.LastName,
                FullName       = e.FirstName + " " + e.LastName,
                e.Email, e.Phone, e.HireDate, e.Salary, e.IsActive,
                e.JobTitle, e.JobLevel, e.CreatedAt, e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                e.ManagerId,
                ManagerName    = e.Manager != null ? e.Manager.FirstName + " " + e.Manager.LastName : null,
                Username       = e.User != null ? e.User.Username : null,
                Role           = e.User != null ? e.User.Role : "Employee"
            })
            .ToListAsync();

        return Ok(employees);
    }

    // ── GET one employee ───────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!CanAccessEmployee(id)) return Forbid();

        var e = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Include(e => e.Subordinates)
            .Include(e => e.Contracts)
            .Include(e => e.Leaves)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (e == null) return NotFound(new { message = $"Employee {id} not found." });

        return Ok(new
        {
            e.Id, e.FirstName, e.LastName,
            FullName       = e.FirstName + " " + e.LastName,
            e.Email, e.Phone, e.HireDate, e.Salary, e.IsActive,
            e.JobTitle, e.JobLevel, e.CreatedAt, e.DepartmentId,
            DepartmentName = e.Department?.Name,
            e.ManagerId,
            ManagerName    = e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}" : null,
            Username       = e.User?.Username,
            Role           = e.User?.Role ?? "Employee",
            SubordinatesCount = e.Subordinates.Count,
            Subordinates   = e.Subordinates.Select(s => new
            {
                s.Id, FullName = s.FirstName + " " + s.LastName, s.JobTitle
            }),
            e.Contracts,
            e.Leaves
        });
    }

    // ── POST create employee ───────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request)
    {
        var validation = await ValidateEmployeeRequest(
            request.FirstName, request.LastName, request.Email, request.ManagerId, null);
        if (validation != null) return validation;

        var role = NormalizeRole(request.Role) ?? "Employee";
        if (!User.IsInRole("Admin")) role = "Employee";

        var providedPassword = string.IsNullOrWhiteSpace(request.Password)
            ? null : request.Password.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var employee = new Employee
        {
            FirstName    = request.FirstName.Trim(),
            LastName     = request.LastName.Trim(),
            Email        = request.Email.Trim(),
            Phone        = request.Phone,
            HireDate     = request.HireDate,
            Salary       = request.Salary,
            DepartmentId = request.DepartmentId,
            JobTitle     = request.JobTitle,
            JobLevel     = request.JobLevel,
            ManagerId    = request.ManagerId,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var username          = await _credentials.GenerateUniqueUsernameAsync(employee.FirstName, employee.LastName);
        var temporaryPassword = providedPassword ?? _credentials.GenerateTemporaryPassword();

        if (providedPassword != null && temporaryPassword.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        _context.Users.Add(new User
        {
            EmployeeId   = employee.Id,
            Username     = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
            Role         = role,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        await _email.SendWelcomeEmailAsync(
            toEmail:           employee.Email,
            toName:            $"{employee.FirstName} {employee.LastName}",
            username:          username,
            temporaryPassword: temporaryPassword);

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, new CreateEmployeeResponse
        {
            EmployeeId        = employee.Id,
            FirstName         = employee.FirstName,
            LastName          = employee.LastName,
            Email             = employee.Email,
            Username          = username,
            TemporaryPassword = temporaryPassword,
            Role              = role
        });
    }

    // ── PUT update employee ────────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null) return NotFound(new { message = $"Employee {id} not found." });

        var validation = await ValidateEmployeeRequest(
            request.FirstName, request.LastName, request.Email, request.ManagerId, id);
        if (validation != null) return validation;

        employee.FirstName    = request.FirstName.Trim();
        employee.LastName     = request.LastName.Trim();
        employee.Email        = request.Email.Trim();
        employee.Phone        = request.Phone;
        employee.HireDate     = request.HireDate;
        employee.Salary       = request.Salary;
        employee.DepartmentId = request.DepartmentId;
        employee.JobTitle     = request.JobTitle;
        employee.JobLevel     = request.JobLevel;
        employee.ManagerId    = request.ManagerId;
        employee.IsActive     = request.IsActive;

        if (employee.User != null)
        {
            employee.User.IsActive = request.IsActive;
            var newRole = NormalizeRole(request.Role);
            if (User.IsInRole("Admin") && newRole != null)
                employee.User.Role = newRole;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ── PUT update own profile ─────────────────────────────
    [HttpPut("{id:int}/profile")]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequest request)
    {
        if (!CanAccessEmployee(id)) return Forbid();

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "First name and last name are required." });

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required." });

        var email = request.Email.Trim();
        if (await _context.Employees.AnyAsync(e => e.Email == email && e.Id != id))
            return Conflict(new { message = "Another employee already uses this email." });

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return NotFound(new { message = $"Employee {id} not found." });

        employee.FirstName = request.FirstName.Trim();
        employee.LastName  = request.LastName.Trim();
        employee.Phone     = request.Phone;
        employee.Email     = email;
        await _context.SaveChangesAsync();

        return Ok(new { employee.Id, employee.FirstName, employee.LastName, employee.Email, employee.Phone });
    }

    // ── DELETE employee ────────────────────────────────────
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .Include(e => e.Subordinates)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null) return NotFound(new { message = $"Employee {id} not found." });

        if (employee.Subordinates.Any(e => e.IsActive))
            return Conflict(new { message = "Reassign active subordinates before deleting." });

        if (employee.User != null) _context.Users.Remove(employee.User);
        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ── POST reset password (by admin/manager) ─────────────
    [HttpPost("{id:int}/reset-password")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetEmployeePasswordRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null) return NotFound(new { message = $"Employee {id} not found." });
        if (employee.User == null) return NotFound(new { message = "This employee has no user account." });

        var useManual         = !string.IsNullOrWhiteSpace(request.NewPassword);
        var temporaryPassword = useManual
            ? request.NewPassword!.Trim()
            : _credentials.GenerateTemporaryPassword();

        if (useManual && temporaryPassword.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        employee.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            employeeId        = employee.Id,
            username          = employee.User.Username,
            temporaryPassword,
            message = useManual ? "Password updated." : "Temporary password generated."
        });
    }

    // ── GET by department ──────────────────────────────────
    [HttpGet("department/{departmentId:int}")]
    public async Task<IActionResult> GetByDepartment(int departmentId) =>
        Ok(await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.DepartmentId == departmentId && e.IsActive)
            .Select(e => new
            {
                e.Id, FullName = e.FirstName + " " + e.LastName, e.JobTitle, e.JobLevel, e.Email
            })
            .ToListAsync());

    // ── GET subordinates ───────────────────────────────────
    [HttpGet("{id:int}/subordinates")]
    public async Task<IActionResult> GetSubordinates(int id)
    {
        if (!CanAccessEmployee(id)) return Forbid();

        return Ok(await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.ManagerId == id && e.IsActive)
            .Select(e => new
            {
                e.Id, FullName = e.FirstName + " " + e.LastName, e.JobTitle, e.JobLevel,
                DepartmentName = e.Department != null ? e.Department.Name : null
            })
            .ToListAsync());
    }

    // ── Helpers ───────────────────────────────────────────
    private async Task<IActionResult?> ValidateEmployeeRequest(
        string firstName, string lastName, string email, int? managerId, int? employeeId)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return BadRequest(new { message = "First name and last name are required." });

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required." });

        var normalizedEmail = email.Trim();
        if (await _context.Employees.AnyAsync(e => e.Email == normalizedEmail && e.Id != employeeId))
            return Conflict(new { message = "An employee with this email already exists." });

        if (managerId == employeeId && employeeId.HasValue)
            return BadRequest(new { message = "An employee cannot be their own manager." });

        if (managerId.HasValue &&
            !await _context.Employees.AnyAsync(e => e.Id == managerId.Value && e.IsActive))
            return BadRequest(new { message = "Selected manager does not exist or is inactive." });

        return null;
    }

    private bool CanAccessEmployee(int employeeId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Manager")) return true;
        var claim = User.FindFirst("employeeId")?.Value;
        return int.TryParse(claim, out var current) && current == employeeId;
    }

    private static string? NormalizeRole(string? role) =>
        AllowedRoles.FirstOrDefault(r =>
            string.Equals(r, role?.Trim(), StringComparison.OrdinalIgnoreCase));
}
