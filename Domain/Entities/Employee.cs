namespace HRManagement.API.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime HireDate { get; set; }
    public decimal? Salary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Department ─────────────────────────────────────────
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    // ── Job ────────────────────────────────────────────────
    public string? JobTitle { get; set; }
    public string? JobLevel { get; set; }

    // ── Hierarchy ──────────────────────────────────────────
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

    // ── Relations ──────────────────────────────────────────
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
    public User? User { get; set; }
}
