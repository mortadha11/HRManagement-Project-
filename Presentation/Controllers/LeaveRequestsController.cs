using HRManagement.API.Application.DTOs;
using HRManagement.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveService;

    public LeaveRequestsController(ILeaveRequestService leaveService)
    {
        _leaveService = leaveService;
    }

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("employeeId")?.Value;
        int.TryParse(claim, out var currentEmpId);
        return currentEmpId;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAll()
    {
        var leaves = await _leaveService.GetAllAsync();
        return Ok(leaves);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyLeaves()
    {
        var empId = GetCurrentEmployeeId();
        var leaves = await _leaveService.GetByEmployeeIdAsync(empId);
        return Ok(leaves);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var leave = await _leaveService.GetByIdAsync(id);
        if (leave == null) return NotFound(new { message = "Leave request not found" });

        var empId = GetCurrentEmployeeId();
        if (leave.EmployeeId != empId && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
            return Forbid();

        return Ok(leave);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequest request)
    {
        try
        {
            var empId = GetCurrentEmployeeId();
            var result = await _leaveService.CreateAsync(request, empId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new { message = "Leave request submitted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateLeaveStatusRequest request)
    {
        try
        {
            var moderatorId = GetCurrentEmployeeId();
            await _leaveService.UpdateStatusAsync(id, request.Status, moderatorId);
            return Ok(new { message = $"Leave request {request.Status} successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Leave request not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var leave = await _leaveService.GetByIdAsync(id);
            if (leave == null) return NotFound(new { message = "Leave request not found" });
            
            var empId = GetCurrentEmployeeId();
            if (leave.EmployeeId != empId && !User.IsInRole("Admin") && !User.IsInRole("Manager"))
                return Forbid();

            // Typically, employees can only delete 'Pending' requests. For simplicity, just allowing it here.
            await _leaveService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Leave request not found" });
        }
    }
}
