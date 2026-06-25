using Microsoft.EntityFrameworkCore;
using HRManagement.API.Domain.Entities;
using HRManagement.API.Domain.Interfaces;
using HRManagement.API.Infrastructure.Data;

namespace HRManagement.API.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public Task<User?> FindByUsernameAsync(string username, bool activeOnly = true) =>
        _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username == username && (!activeOnly || u.IsActive));

    public Task<User?> FindByEmployeeIdAsync(int employeeId) =>
        _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

    public Task<bool> UsernameExistsAsync(string username) =>
        _context.Users.AnyAsync(u => u.Username == username);

    public Task<bool> EmployeeHasAccountAsync(int employeeId) =>
        _context.Users.AnyAsync(u => u.EmployeeId == employeeId);

    public async Task AddAsync(User user) => await _context.Users.AddAsync(user);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
