using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRManagement.API.Data;
using HRManagement.API.Models;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;
    public EmployeesController(AppDbContext context) => _context = context;

    // GET /api/employees
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Include(e => e.User)
            .Where(e => e.IsActive)
            .Select(e => new {
                e.Id,
                e.FirstName,
                e.LastName,
                FullName       = e.FirstName + " " + e.LastName,
                e.Email,
                e.Phone,
                e.HireDate,
                e.Salary,
                e.IsActive,
                e.JobTitle,
                e.JobLevel,
                e.CreatedAt,
                DepartmentId   = e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : null,
                ManagerId      = e.ManagerId,
                ManagerName    = e.Manager != null ? e.Manager.FirstName + " " + e.Manager.LastName : null,
                Role           = e.User != null ? e.User.Role : "Employee"
            })
            .ToListAsync());

    // GET /api/employees/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Include(e => e.Subordinates)
            .Include(e => e.Contracts)
            .Include(e => e.Leaves)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (e == null) return NotFound(new { message = $"Employe {id} introuvable." });

        return Ok(new {
            e.Id, e.FirstName, e.LastName,
            FullName       = e.FirstName + " " + e.LastName,
            e.Email, e.Phone, e.HireDate, e.Salary, e.IsActive,
            e.JobTitle, e.JobLevel, e.CreatedAt,
            DepartmentId   = e.DepartmentId,
            DepartmentName = e.Department != null ? e.Department.Name : null,
            ManagerId      = e.ManagerId,
            ManagerName    = e.Manager != null ? e.Manager.FirstName + " " + e.Manager.LastName : null,
            Role           = e.User != null ? e.User.Role : "Employee",
            SubordinatesCount = e.Subordinates.Count,
            Subordinates   = e.Subordinates.Select(s => new {
                s.Id,
                FullName = s.FirstName + " " + s.LastName,
                s.JobTitle
            }),
            e.Contracts,
            e.Leaves
        });
    }

    // POST /api/employees
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest req)
    {
        if (await _context.Employees.AnyAsync(e => e.Email == req.Email))
            return Conflict(new { message = "Un employe avec cet email existe deja." });

        var employee = new Employee {
            FirstName    = req.FirstName,
            LastName     = req.LastName,
            Email        = req.Email,
            Phone        = req.Phone,
            HireDate     = req.HireDate,
            Salary       = req.Salary,
            DepartmentId = req.DepartmentId,
            JobTitle     = req.JobTitle,
            JobLevel     = req.JobLevel,
            ManagerId    = req.ManagerId,
            IsActive     = true,
            CreatedAt    = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, new { id = employee.Id });
    }

    // PUT /api/employees/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeRequest req)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return NotFound(new { message = $"Employe {id} introuvable." });

        employee.FirstName    = req.FirstName;
        employee.LastName     = req.LastName;
        employee.Email        = req.Email;
        employee.Phone        = req.Phone;
        employee.HireDate     = req.HireDate;
        employee.Salary       = req.Salary;
        employee.DepartmentId = req.DepartmentId;
        employee.JobTitle     = req.JobTitle;
        employee.JobLevel     = req.JobLevel;
        employee.ManagerId    = req.ManagerId;
        employee.IsActive     = req.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PUT /api/employees/{id}/profile  ← employee modifie uniquement son propre profil
    [HttpPut("{id}/profile")]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequest req)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return NotFound(new { message = $"Employe {id} introuvable." });

        employee.Phone = req.Phone;
        employee.Email = req.Email;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/employees/{id} - soft delete
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return NotFound(new { message = $"Employe {id} introuvable." });
        employee.IsActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/employees/department/{id}
    [HttpGet("department/{departmentId}")]
    public async Task<IActionResult> GetByDepartment(int departmentId) =>
        Ok(await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.DepartmentId == departmentId && e.IsActive)
            .Select(e => new {
                e.Id,
                FullName = e.FirstName + " " + e.LastName,
                e.JobTitle, e.JobLevel, e.Email
            })
            .ToListAsync());

    // GET /api/employees/{id}/subordinates
    [HttpGet("{id}/subordinates")]
    public async Task<IActionResult> GetSubordinates(int id) =>
        Ok(await _context.Employees
            .Include(e => e.Department)
            .Where(e => e.ManagerId == id && e.IsActive)
            .Select(e => new {
                e.Id,
                FullName = e.FirstName + " " + e.LastName,
                e.JobTitle, e.JobLevel,
                DepartmentName = e.Department != null ? e.Department.Name : null
            })
            .ToListAsync());
}

// ── Request Models ────────────────────────────────────────────────────────────

public class CreateEmployeeRequest
{
    public string    FirstName    { get; set; } = string.Empty;
    public string    LastName     { get; set; } = string.Empty;
    public string    Email        { get; set; } = string.Empty;
    public string?   Phone        { get; set; }
    public DateTime  HireDate     { get; set; }
    public decimal?  Salary       { get; set; }
    public int?      DepartmentId { get; set; }
    public string?   JobTitle     { get; set; }
    public string?   JobLevel     { get; set; }
    public int?      ManagerId    { get; set; }
}

public class UpdateEmployeeRequest : CreateEmployeeRequest
{
    public bool IsActive { get; set; } = true;
}

public class UpdateProfileRequest
{
    public string  Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}