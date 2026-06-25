using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRManagement.API.Domain.Entities;
using HRManagement.API.Infrastructure.Data;

namespace HRManagement.API.Presentation.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    public DepartmentsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _context.Departments.ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dept = await _context.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);
        return dept == null ? NotFound() : Ok(dept);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Department dept)
    {
        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = dept.Id }, dept);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Department dept)
    {
        if (id != dept.Id) return BadRequest();
        _context.Entry(dept).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept == null) return NotFound();
        _context.Departments.Remove(dept);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
