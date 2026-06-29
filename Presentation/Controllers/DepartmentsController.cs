using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRManagement.API.Domain.Entities;
using HRManagement.API.Infrastructure.Data;

namespace HRManagement.API.Presentation.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    public DepartmentsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _context.Departments
            .OrderBy(d => d.Name)
            .ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dept = await _context.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);
        return dept == null ? NotFound() : Ok(dept);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(Department dept)
    {
        var validation = await ValidateDepartment(dept);
        if (validation != null) return validation;

        dept.Name = dept.Name.Trim();
        dept.Description = string.IsNullOrWhiteSpace(dept.Description)
            ? null
            : dept.Description.Trim();

        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = dept.Id }, dept);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, Department dept)
    {
        if (id != dept.Id) return BadRequest();

        var existing = await _context.Departments.FindAsync(id);
        if (existing == null) return NotFound(new { message = $"Department {id} not found." });

        var validation = await ValidateDepartment(dept, id);
        if (validation != null) return validation;

        existing.Name = dept.Name.Trim();
        existing.Description = string.IsNullOrWhiteSpace(dept.Description)
            ? null
            : dept.Description.Trim();

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept == null) return NotFound(new { message = $"Department {id} not found." });

        try
        {
            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Could not delete this department. Check database foreign key migration." });
        }

        return NoContent();
    }

    private async Task<IActionResult?> ValidateDepartment(Department dept, int? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(dept.Name))
            return BadRequest(new { message = "Department name is required." });

        var name = dept.Name.Trim();
        var exists = await _context.Departments.AnyAsync(d =>
            d.Name == name && (!currentId.HasValue || d.Id != currentId.Value));

        if (exists)
            return Conflict(new { message = "A department with this name already exists." });

        return null;
    }
}
