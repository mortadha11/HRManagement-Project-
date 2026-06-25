using HRManagement.API.Domain.Entities;

namespace HRManagement.API.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
