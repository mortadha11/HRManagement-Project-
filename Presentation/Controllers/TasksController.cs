using System.Security.Claims;
using HRManagement.API.Application.DTOs;
using HRManagement.API.Domain.Entities;
using HRManagement.API.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.API.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;

    public TasksController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("employeeId")?.Value;
        int.TryParse(claim, out var empId);
        return empId;
    }

    private bool IsAdminOrManager()
    {
        return User.IsInRole("Admin") || User.IsInRole("Manager");
    }

    // GET /api/tasks/assigned
    [HttpGet("assigned")]
    public async Task<IActionResult> GetAssignedTasks()
    {
        var empId = GetCurrentEmployeeId();
        var tasks = await _context.Tasks
            .Include(t => t.Manager)
            .Where(t => t.EmployeeId == empId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new {
                t.Id, t.Title, t.Description, t.Status, t.DueDate, t.CreatedAt,
                t.PriorityLevel,
                t.ManagerId,
                ManagerName = t.Manager.FirstName + " " + t.Manager.LastName
            })
            .ToListAsync();

        return Ok(tasks);
    }

    // GET /api/tasks/created
    [HttpGet("created")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetCreatedTasks()
    {
        var empId = GetCurrentEmployeeId();
        
        var query = _context.Tasks.Include(t => t.Assignee).AsQueryable();
        
        // Admin sees all, Manager sees tasks they created
        if (!User.IsInRole("Admin")) 
        {
            query = query.Where(t => t.ManagerId == empId);
        }

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new {
                t.Id, t.Title, t.Description, t.Status, t.DueDate, t.CreatedAt,
                t.PriorityLevel,
                t.EmployeeId,
                AssigneeName = t.Assignee.FirstName + " " + t.Assignee.LastName
            })
            .ToListAsync();

        return Ok(tasks);
    }

    // POST /api/tasks
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Task title is required." });

        if (!await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId && e.IsActive))
            return BadRequest(new { message = "Assignee employee does not exist or is inactive." });

        var managerId = GetCurrentEmployeeId();

        var task = new EmployeeTask
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Status = "Pending",
            PriorityLevel = string.IsNullOrWhiteSpace(request.PriorityLevel) ? "Low" : request.PriorityLevel,
            DueDate = request.DueDate,
            ManagerId = managerId,
            EmployeeId = request.EmployeeId
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCreatedTasks), null, new { message = "Task created successfully.", taskId = task.Id });
    }

    // PUT /api/tasks/{id}
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return NotFound(new { message = "Task not found." });

        var adminOrCreator = User.IsInRole("Admin") || task.ManagerId == GetCurrentEmployeeId();
        if (!adminOrCreator) return Forbid();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required." });

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.DueDate = request.DueDate;
        task.PriorityLevel = request.PriorityLevel ?? task.PriorityLevel;
        task.Status = string.IsNullOrWhiteSpace(request.Status) ? task.Status : request.Status;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Task updated." });
    }

    // PUT /api/tasks/{id}/status
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return NotFound(new { message = "Task not found." });

        var empId = GetCurrentEmployeeId();
        var canUpdate = User.IsInRole("Admin") || task.ManagerId == empId || task.EmployeeId == empId;
        if (!canUpdate) return Forbid();

        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "Status is required." });

        task.Status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Status updated." });
    }

    // DELETE /api/tasks/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return NotFound(new { message = "Task not found." });

        var adminOrCreator = User.IsInRole("Admin") || task.ManagerId == GetCurrentEmployeeId();
        if (!adminOrCreator) return Forbid();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
