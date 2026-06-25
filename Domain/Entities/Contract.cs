namespace HRManagement.API.Domain.Entities;

public class Contract
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Salary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Employee? Employee { get; set; }
}
