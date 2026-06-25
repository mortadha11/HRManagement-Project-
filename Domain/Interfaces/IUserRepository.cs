using HRManagement.API.Domain.Entities;

namespace HRManagement.API.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByUsernameAsync(string username, bool activeOnly = true);
    Task<User?> FindByEmployeeIdAsync(int employeeId);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmployeeHasAccountAsync(int employeeId);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
