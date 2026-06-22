namespace HRManagement.API.Models;

public class User
{
    public int Id { get; set; }

    // ← Lien vers Employees (Option B)
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // 'Admin', 'Manager', 'Employee'
    public string Role { get; set; } = "Employee";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}