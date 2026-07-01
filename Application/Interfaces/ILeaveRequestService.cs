using HRManagement.API.Application.DTOs;

namespace HRManagement.API.Application.Interfaces;

public interface ILeaveRequestService
{
    Task<IEnumerable<LeaveDto>> GetAllAsync();
    Task<LeaveDto?> GetByIdAsync(int id);
    Task<IEnumerable<LeaveDto>> GetByEmployeeIdAsync(int employeeId);
    
    Task<LeaveDto> CreateAsync(CreateLeaveRequest request, int employeeId);
    Task UpdateStatusAsync(int id, string status, int moderatorId);
    Task DeleteAsync(int id);
}
