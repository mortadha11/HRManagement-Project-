using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRManagement.API.Domain.Entities;
using HRManagement.API.Infrastructure.Data;

namespace HRManagement.API.Presentation.Controllers;

[ApiController]
[Route("api/leaves")]
public class LeavesController : ControllerBase
{
    private readonly AppDbContext _context;
    public LeavesController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _context.Leaves.Include(l => l.Employee).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var leave = await _context.Leaves
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == id);
        return leave == null ? NotFound() : Ok(leave);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Leave leave)
    {
        _context.Leaves.Add(leave);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = leave.Id }, leave);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Leave leave)
    {
        if (id != leave.Id) return BadRequest();
        _context.Entry(leave).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var leave = await _context.Leaves.FindAsync(id);
        if (leave == null) return NotFound();
        leave.Status = status;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var leave = await _context.Leaves.FindAsync(id);
        if (leave == null) return NotFound();
        _context.Leaves.Remove(leave);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
