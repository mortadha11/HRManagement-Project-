using HRManagement.API.Application.DTOs;
using HRManagement.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContractsController : ControllerBase
{
    private readonly IContractService _contractService;

    public ContractsController(IContractService contractService)
    {
        _contractService = contractService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAll()
    {
        var contracts = await _contractService.GetAllAsync();
        return Ok(contracts);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetById(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null) return NotFound(new { message = "Contract not found" });
        return Ok(contract);
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        // Add basic authorization: only Admin/Manager or the Employee themselves
        var claim = User.FindFirst("employeeId")?.Value;
        int.TryParse(claim, out var currentEmpId);
        
        var canAccess = User.IsInRole("Admin") || User.IsInRole("Manager") || currentEmpId == employeeId;
        if (!canAccess) return Forbid();

        var contracts = await _contractService.GetByEmployeeIdAsync(employeeId);
        return Ok(contracts);
    }

    [HttpGet("expiring")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetExpiring([FromQuery] int days = 30)
    {
        var contracts = await _contractService.GetExpiringSoonAsync(days);
        return Ok(contracts);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateContractRequest request)
    {
        try
        {
            var result = await _contractService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new { message = "Contract created successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContractRequest request)
    {
        try
        {
            await _contractService.UpdateAsync(id, request);
            return Ok(new { message = "Contract updated successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Contract not found" });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _contractService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Contract not found" });
        }
    }
}
