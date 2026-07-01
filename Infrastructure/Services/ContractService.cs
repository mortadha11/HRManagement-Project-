using HRManagement.API.Application.DTOs;
using HRManagement.API.Application.Interfaces;
using HRManagement.API.Domain.Entities;
using HRManagement.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HRManagement.API.Infrastructure.Services;

public class ContractService : IContractService
{
    private readonly AppDbContext _context;

    public ContractService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ContractDto>> GetAllAsync()
    {
        await ExpireOldContractsAsync(); // Auto-update statuses safely on read
        return await _context.Contracts
            .Include(c => c.Employee)
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    public async Task<ContractDto?> GetByIdAsync(int id)
    {
        var contract = await _context.Contracts
            .Include(c => c.Employee)
            .FirstOrDefaultAsync(c => c.Id == id);
            
        return contract == null ? null : MapToDto(contract);
    }

    public async Task<IEnumerable<ContractDto>> GetByEmployeeIdAsync(int employeeId)
    {
        await ExpireOldContractsAsync();
        return await _context.Contracts
            .Include(c => c.Employee)
            .Where(c => c.EmployeeId == employeeId)
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    public async Task<IEnumerable<ContractDto>> GetExpiringSoonAsync(int daysAhead)
    {
        await ExpireOldContractsAsync();
        var targetDate = DateTime.UtcNow.AddDays(daysAhead);
        
        return await _context.Contracts
            .Include(c => c.Employee)
            .Where(c => c.Status == "Active" && c.EndDate.HasValue && c.EndDate.Value <= targetDate)
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    public async Task<ContractDto> CreateAsync(CreateContractRequest request)
    {
        // Business Rule: An employee can have only ONE active contract at a time
        var hasActive = await _context.Contracts
            .AnyAsync(c => c.EmployeeId == request.EmployeeId && c.Status == "Active");
            
        if (hasActive)
            throw new InvalidOperationException("This employee already has an active contract.");

        var contract = new Contract
        {
            EmployeeId = request.EmployeeId,
            Type = request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Salary = request.Salary,
            Position = request.Position,
            WorkingHours = request.WorkingHours,
            Status = "Active" // Freshly created are typically Active
        };

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(contract.Id) ?? MapToDto(contract);
    }

    public async Task UpdateAsync(int id, UpdateContractRequest request)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null)
            throw new KeyNotFoundException("Contract not found.");

        contract.Type = request.Type;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.Salary = request.Salary;
        contract.Position = request.Position;
        contract.WorkingHours = request.WorkingHours;
        contract.Status = request.Status;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null)
            throw new KeyNotFoundException("Contract not found.");

        _context.Contracts.Remove(contract);
        await _context.SaveChangesAsync();
    }

    // ── Helper Methods ──────────────────────────────────────────

    private static ContractDto MapToDto(Contract c)
    {
        return new ContractDto
        {
            Id = c.Id,
            EmployeeId = c.EmployeeId,
            EmployeeName = c.Employee != null ? $"{c.Employee.FirstName} {c.Employee.LastName}" : "Unknown",
            Type = c.Type,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            Salary = c.Salary,
            Position = c.Position,
            WorkingHours = c.WorkingHours,
            Status = c.Status,
            CreatedAt = c.CreatedAt
        };
    }

    // Rule: Auto-update status to "Expired" when end date passes
    private async Task ExpireOldContractsAsync()
    {
        var now = DateTime.UtcNow.Date;
        var expiredContracts = await _context.Contracts
            .Where(c => c.Status == "Active" && c.EndDate.HasValue && c.EndDate.Value.Date < now)
            .ToListAsync();

        if (expiredContracts.Any())
        {
            foreach (var c in expiredContracts)
            {
                c.Status = "Expired";
            }
            await _context.SaveChangesAsync();
        }
    }
}
