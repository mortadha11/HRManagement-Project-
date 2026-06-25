namespace HRManagement.API.Application.Interfaces;

public interface ICredentialGeneratorService
{
    Task<string> GenerateUniqueUsernameAsync(string firstName, string lastName);
    string GenerateTemporaryPassword(int length = 10);
}
