using Microsoft.EntityFrameworkCore;
using HRManagement.API.Application.Interfaces;
using HRManagement.API.Infrastructure.Data;

namespace HRManagement.API.Infrastructure.Services;

public class CredentialGeneratorService : ICredentialGeneratorService
{
    private readonly AppDbContext _context;
    private static readonly Random _random = new();

    public CredentialGeneratorService(AppDbContext context) => _context = context;

    public async Task<string> GenerateUniqueUsernameAsync(string firstName, string lastName)
    {
        var baseUsername = $"{firstName}.{lastName}"
            .ToLowerInvariant()
            .Replace(" ", "")
            .Trim();

        baseUsername = new string(baseUsername
            .Where(c => char.IsLetterOrDigit(c) || c == '.')
            .ToArray());

        var username = baseUsername;
        var suffix   = 1;

        while (await _context.Users.AnyAsync(u => u.Username == username))
        {
            suffix++;
            username = $"{baseUsername}{suffix}";
        }

        return username;
    }

    public string GenerateTemporaryPassword(int length = 10)
    {
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower   = "abcdefghijkmnpqrstuvwxyz";
        const string digits  = "23456789";
        const string symbols = "!@#$%";

        var all   = upper + lower + digits + symbols;
        var chars = new char[length];

        chars[0] = upper[_random.Next(upper.Length)];
        chars[1] = lower[_random.Next(lower.Length)];
        chars[2] = digits[_random.Next(digits.Length)];
        chars[3] = symbols[_random.Next(symbols.Length)];

        for (int i = 4; i < length; i++)
            chars[i] = all[_random.Next(all.Length)];

        return new string(chars.OrderBy(_ => _random.Next()).ToArray());
    }
}
