using HRManagement.API.Application.DTOs;

namespace HRManagement.API.Application.Interfaces;

public interface IContractService
{
    Task<IEnumerable<ContractDto>> GetAllAsync();
    Task<ContractDto?> GetByIdAsync(int id);
    Task<IEnumerable<ContractDto>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<ContractDto>> GetExpiringSoonAsync(int daysAhead);
    
    Task<ContractDto> CreateAsync(CreateContractRequest request);
    Task UpdateAsync(int id, UpdateContractRequest request);
    Task DeleteAsync(int id);
}
