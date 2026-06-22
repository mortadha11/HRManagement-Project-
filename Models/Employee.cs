namespace HRManagement.API.Models;

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

    // ── Département ───────────────────────────────────────
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    // ── Nouveau : Poste & Hiérarchie ──────────────────────
    public string? JobTitle { get; set; }
    // Ex: 'Développeur Senior', 'RH Manager', 'CTO'

    public string? JobLevel { get; set; }
    // 'Junior', 'Mid', 'Senior', 'Lead', 'Director'

    // Auto-référence : son manager direct
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }

    // Les employés qu'il manage
    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

    // ── Relations existantes ──────────────────────────────
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Leave> Leaves { get; set; } = new List<Leave>();

    // ── Lien vers son compte User (auth) ──────────────────
    public User? User { get; set; }
}