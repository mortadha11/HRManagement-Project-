using Microsoft.EntityFrameworkCore;
using HRManagement.API.Domain.Entities;
using HRManagement.API.Domain.Interfaces;
using HRManagement.API.Infrastructure.Data;

namespace HRManagement.API.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context) => _context = context;

    public Task<List<Employee>> GetAllActiveAsync() =>
        _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Include(e => e.User)
            .Where(e => e.IsActive)
            .ToListAsync();

    public Task<Employee?> FindByIdAsync(int id, bool includeRelations = false)
    {
        var query = _context.Employees.AsQueryable();

        if (includeRelations)
        {
            query = query
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .Include(e => e.Subordinates)
                .Include(e => e.Contracts)
                .Include(e => e.Leaves)
                .Include(e => e.User);
        }

        return query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<bool> EmailExistsAsync(string email, int? excludeId = null) =>
        _context.Employees.AnyAsync(e => e.Email == email && e.Id != excludeId);

    public Task<bool> ManagerExistsAndActiveAsync(int managerId) =>
        _context.Employees.AnyAsync(e => e.Id == managerId && e.IsActive);

    public async Task AddAsync(Employee employee) => await _context.Employees.AddAsync(employee);

    public void Update(Employee employee) => _context.Employees.Update(employee);

    public void Remove(Employee employee) => _context.Employees.Remove(employee);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
