namespace HRManagement.API.Domain.Entities;

public class Contract
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    
    public string Type { get; set; } = string.Empty; // CDI, CDD, Internship, Freelance
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Salary { get; set; }
    
    public string? Position { get; set; }
    public int? WorkingHours { get; set; } // e.g. 40 hours per week
    
    public string Status { get; set; } = "Active"; // Active, Expired, Terminated

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Employee? Employee { get; set; }
}
