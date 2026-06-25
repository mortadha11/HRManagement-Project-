using HRManagement.API.Domain.Entities;

namespace HRManagement.API.Domain.Interfaces;

public interface IEmployeeRepository
{
    Task<List<Employee>> GetAllActiveAsync();
    Task<Employee?> FindByIdAsync(int id, bool includeRelations = false);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    Task<bool> ManagerExistsAndActiveAsync(int managerId);
    Task AddAsync(Employee employee);
    void Update(Employee employee);
    void Remove(Employee employee);
    Task SaveChangesAsync();
}
