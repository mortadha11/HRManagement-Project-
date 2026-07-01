using HRManagement.API.Application.DTOs;
using HRManagement.API.Application.Interfaces;
using HRManagement.API.Domain.Entities;
using HRManagement.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.API.Infrastructure.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly AppDbContext _context;

    public LeaveRequestService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LeaveDto>> GetAllAsync()
    {
        return await _context.Leaves
            .Include(l => l.Employee)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => MapToDto(l))
            .ToListAsync();
    }

    public async Task<LeaveDto?> GetByIdAsync(int id)
    {
        var leave = await _context.Leaves
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == id);
            
        return leave == null ? null : MapToDto(leave);
    }

    public async Task<IEnumerable<LeaveDto>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.Leaves
            .Include(l => l.Employee)
            .Where(l => l.EmployeeId == employeeId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => MapToDto(l))
            .ToListAsync();
    }

    public async Task<LeaveDto> CreateAsync(CreateLeaveRequest request, int employeeId)
    {
        // Validation: cannot request leave overlapping an existing approved/pending request
        var overlapping = await _context.Leaves
            .AnyAsync(l => l.EmployeeId == employeeId 
                        && (l.Status == "Pending" || l.Status == "Approved")
                        && l.StartDate <= request.EndDate
                        && request.StartDate <= l.EndDate);
                        
        if (overlapping)
            throw new InvalidOperationException("This leave request overlaps with an existing pending or approved request.");

        int daysRequested = CalculateWorkingDays(request.StartDate, request.EndDate);

        var leave = new Leave
        {
            EmployeeId = employeeId,
            Type = request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DaysRequested = daysRequested,
            Status = "Pending",
            Reason = request.Reason
        };

        _context.Leaves.Add(leave);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(leave.Id) ?? MapToDto(leave);
    }

    public async Task UpdateStatusAsync(int id, string status, int moderatorId)
    {
        var leave = await _context.Leaves.FindAsync(id);
        if (leave == null)
            throw new KeyNotFoundException("Leave request not found.");

        if (status != "Approved" && status != "Rejected")
            throw new InvalidOperationException("Status must be Approved or Rejected.");

        leave.Status = status;
        leave.ModeratedAt = DateTime.UtcNow;
        leave.ModeratedById = moderatorId;

        // If you want robust balance tracking, you would decrement a generic "VacationDays" property on Employee here.
        
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var leave = await _context.Leaves.FindAsync(id);
        if (leave == null)
            throw new KeyNotFoundException("Leave request not found.");

        _context.Leaves.Remove(leave);
        await _context.SaveChangesAsync();
    }

    // ── Helper Methods ──────────────────────────────────────────

    private static LeaveDto MapToDto(Leave l)
    {
        return new LeaveDto
        {
            Id = l.Id,
            EmployeeId = l.EmployeeId,
            EmployeeName = l.Employee != null ? $"{l.Employee.FirstName} {l.Employee.LastName}" : "Unknown",
            Type = l.Type,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            DaysRequested = l.DaysRequested,
            Status = l.Status,
            Reason = l.Reason,
            ModeratedAt = l.ModeratedAt,
            ModeratedById = l.ModeratedById,
            CreatedAt = l.CreatedAt
        };
    }

    // Auto-calculate number of days requested (excluding weekends)
    private static int CalculateWorkingDays(DateTime start, DateTime end)
    {
        int days = 0;
        var current = start.Date;
        var target = end.Date;
        
        while (current <= target)
        {
            if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
            {
                days++;
            }
            current = current.AddDays(1);
        }
        
        return days;
    }
}
