using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRManagement.API.Domain.Entities;
using HRManagement.API.Infrastructure.Data;

namespace HRManagement.API.Presentation.Controllers;

[ApiController]
[Route("api/contracts")]
public class ContractsController : ControllerBase
{
    private readonly AppDbContext _context;
    public ContractsController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _context.Contracts.Include(c => c.Employee).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contract = await _context.Contracts
            .Include(c => c.Employee)
            .FirstOrDefaultAsync(c => c.Id == id);
        return contract == null ? NotFound() : Ok(contract);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Contract contract)
    {
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = contract.Id }, contract);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Contract contract)
    {
        if (id != contract.Id) return BadRequest();
        _context.Entry(contract).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null) return NotFound();
        _context.Contracts.Remove(contract);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
